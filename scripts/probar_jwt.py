"""Pruebas HTTP contra SQL Server real; genera evidencias sin claves ni tokens reutilizables."""
import base64
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timezone
import hashlib
import hmac
import html
import json
import os
from pathlib import Path
import secrets
import subprocess
import time
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen
import uuid

ROOT = Path(__file__).resolve().parents[1]
BASE = 'http://localhost:5131'
OUTPUT = ROOT / 'artifacts/s14'
CHECKS = []
TOKENS = {}
TEST_KEY = secrets.token_urlsafe(48)

def sql(query):
    result = subprocess.run(['sqlcmd', '-S', os.environ.get('VETERINARIA_TEST_SERVIDOR', r'(localdb)\MSSQLLocalDB'),
        '-E', '-d', os.environ.get('VETERINARIA_TEST_BD', 'VeterinariaDB'), '-b', '-h', '-1', '-W',
        '-Q', 'SET NOCOUNT ON; ' + query], check=True, capture_output=True, text=True)
    return result.stdout.strip()

def clean(value):
    if isinstance(value, dict):
        return {k: ('[omitido por seguridad]' if k.lower() in ['clave', 'token', 'clavehash'] else clean(v)) for k,v in value.items()}
    if isinstance(value, list):
        return [clean(v) for v in value]
    return value

def request(method, path, expected, payload=None, role=None, label=None, token=None):
    data = None if payload is None else json.dumps(payload).encode('utf-8')
    headers = {'Content-Type': 'application/json'}
    current = token if token is not None else TOKENS.get(role)
    if current:
        headers['Authorization'] = 'Bearer ' + current
    req = Request(BASE + path, data=data, method=method, headers=headers)
    start = time.perf_counter()
    try:
        response = urlopen(req, timeout=20)
    except HTTPError as error:
        response = error
    raw = response.read().decode('utf-8')
    assert response.status == expected, f'{method} {path} [{role}]: esperaba {expected}, recibí {response.status}: {raw}'
    body = json.loads(raw) if raw else None
    record = {'metodo': method, 'url': BASE + path, 'rol': role or ('Token suministrado' if token else 'Sin token'),
        'estado': response.status, 'solicitud': clean(payload), 'respuesta': clean(body),
        'fechaUTC': datetime.now(timezone.utc).isoformat(), 'duracionMs': round((time.perf_counter()-start)*1000),
        'cabeceras': {k:v for k,v in response.headers.items() if k.lower() in ['content-type', 'location', 'www-authenticate']}}
    if label:
        record['evidencia'] = label
    CHECKS.append(record)
    print(f'OK {expected} {method} {path} [{record["rol"]}]')
    return body, response.headers

def decode(token):
    parts = token.split('.')
    return json.loads(base64.urlsafe_b64decode(parts[1] + '=' * (-len(parts[1]) % 4)))

def signed(payload, key=TEST_KEY):
    def b64(value):
        return base64.urlsafe_b64encode(json.dumps(value, separators=(',', ':')).encode()).decode().rstrip('=')
    content = b64({'alg':'HS256', 'typ':'JWT'}) + '.' + b64(payload)
    signature = base64.urlsafe_b64encode(hmac.new(key.encode(), content.encode(), hashlib.sha256).digest()).decode().rstrip('=')
    return content + '.' + signature

def reports():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    (OUTPUT/'resultado-pruebas.json').write_text(json.dumps({'resultado':'Correcto', 'total':len(CHECKS), 'pruebas':CHECKS}, ensure_ascii=False, indent=2), encoding='utf-8')
    style = '''body{font-family:Segoe UI,Arial;margin:40px auto;max-width:1050px;padding:0 24px;color:#172b3a;background:#f7f9fb}h1{font-size:26px;margin:8px 0}h2{font-size:17px;margin-bottom:8px}header{border-bottom:2px solid #186a79;padding-bottom:18px}small{color:#526575}pre{background:white;border:1px solid #d5dfe4;border-radius:6px;padding:18px;font:15px/1.5 Consolas,monospace;white-space:pre-wrap;overflow-wrap:anywhere}b{color:#126575}.status{font-size:28px;font-weight:700;margin:16px 0}.meta{line-height:1.8}a{color:#126575}'''
    def save(name, title, content):
        (OUTPUT/(name+'.html')).write_text('<!doctype html><html lang="es"><meta charset="utf-8"><title>'+html.escape(title)+'</title><style>'+style+'</style><header><small>VETERINARIA APP · LABORATORIO 14 · CLIENTE HTTP PYTHON</small><h1>'+html.escape(title)+'</h1></header>'+content+'</html>', encoding='utf-8')
    links = []
    for r in CHECKS:
        if 'evidencia' not in r:
            continue
        name = r['evidencia']
        title = f'{r["estado"]} · {r["rol"]} · {r["metodo"]}'
        esc = lambda val: html.escape(json.dumps(val, ensure_ascii=False, indent=2))
        content = f'<div class="status">HTTP {r["estado"]} · verificado</div><div class="meta"><b>{r["metodo"]}</b> {html.escape(r["url"])}<br>Identidad: <b>{html.escape(r["rol"])}</b><br>Fecha UTC: {r["fechaUTC"]} · {r["duracionMs"]} ms</div>'
        if r['solicitud'] is not None:
            content += '<h2>Cuerpo enviado</h2><pre>'+esc(r['solicitud'])+'</pre>'
        content += '<h2>Respuesta recibida</h2><pre>'+(esc(r['respuesta']) if r['respuesta'] is not None else '(sin cuerpo de respuesta)')+'</pre>'
        if r['cabeceras']:
            content += '<h2>Encabezados recibidos</h2><pre>'+esc(r['cabeceras'])+'</pre>'
        save(name, title, content)
        links.append(f'<li><a href="{name}.html">{html.escape(name)} · {html.escape(title)}</a></li>')
    for role, token in TOKENS.items():
        name = 'token-'+role.lower()
        claims = decode(token)
        content = '<p>Token obtenido con POST /api/auth/login y aceptado por GET /api/auth/perfil. Decodificación local; la clave de firma nunca se muestra.</p><h2>Encabezado</h2><pre>{\n  "alg": "HS256",\n  "typ": "JWT"\n}</pre><h2>Claims del token real</h2><pre>'+html.escape(json.dumps(claims, indent=2, ensure_ascii=False))+'</pre><p>El token completo se omite porque permite acceder a la API durante su vigencia.</p>'
        save(name, 'JWT decodificado · '+role, content)
        (OUTPUT/(name+'.json')).write_text(json.dumps(claims, indent=2), encoding='utf-8')
        links.append(f'<li><a href="{name}.html">JWT decodificado · {role}</a></li>')
    save('index', 'Evidencias de las pruebas JWT', f'<p>{len(CHECKS)} solicitudes comprobadas contra la API y SQL Server reales. Se verificaron 15 operaciones y los códigos 200, 201, 204, 400, 401, 403, 404 y 409.</p><ul>'+''.join(links)+'</ul>')

def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    unique = uuid.uuid4().hex[:12]
    test_email = f'prueba-{unique}@veterinaria.test'
    parallel_email = f'paralela-{unique}@veterinaria.test'
    client_id = pet_id = other_id = None
    env = dict(os.environ, ASPNETCORE_ENVIRONMENT='Development', ASPNETCORE_URLS=BASE,
        Jwt__Clave=TEST_KEY, Jwt__MinutosExpiracion='30')
    with (OUTPUT/'api-pruebas.log').open('w', encoding='utf-8') as log:
        process = subprocess.Popen(['dotnet', str(ROOT/'VeterinariaApp.Api/bin/Debug/net10.0/VeterinariaApp.Api.dll')],
            cwd=ROOT/'VeterinariaApp.Api', env=env, stdout=log, stderr=log)
        try:
            for _ in range(80):
                if process.poll() is not None:
                    raise RuntimeError('La API no inició. Revisar artifacts/s14/api-pruebas.log.')
                try:
                    with urlopen(BASE+'/swagger/v1/swagger.json', timeout=1) as response:
                        spec = json.load(response)
                    break
                except (URLError, TimeoutError):
                    time.sleep(.25)
            else:
                raise TimeoutError('La API no inició a tiempo.')
            assert sum(len([m for m in methods if m in ['get','post','put','delete']]) for methods in spec['paths'].values()) == 15
            assert spec['components']['securitySchemes']['Bearer']['scheme'] == 'bearer'
            for path in ['/api/auth/login', '/api/auth/registro']:
                assert not spec['paths'][path]['post'].get('security')
            assert spec['paths']['/api/auth/perfil']['get']['security'][0]['Bearer'] == []

            for role, email in [('Recepcionista','recepcion@veterinaria.test'),('Veterinario','veterinario@veterinaria.test')]:
                login, _ = request('POST','/api/auth/login',200,{'correo':email,'clave':'Veterinaria2026!'}, label='login-'+role.lower())
                TOKENS[role] = login['token']
                claims = decode(login['token'])
                assert claims['rol'] == role and claims['email'] == email
                assert all(c in claims for c in ['sub','email','rol','jti','iss','aud','nbf','exp'])
                assert 'clave' not in claims and 'claveHash' not in claims
                assert claims['exp']-claims['nbf'] == 1800
                profile,_ = request('GET','/api/auth/perfil',200,role=role,label='perfil-'+role.lower())
                assert profile == {'correo':email,'rol':role}

            registration={'correo':test_email,'clave':'PruebaSegura2026!'}
            u,_=request('POST','/api/auth/registro',201,registration,label='registro-201')
            assert u['rol']=='Recepcionista' and 'claveHash' not in u and 'clave' not in u
            stored=sql(f"SELECT ClaveHash FROM dbo.Usuarios WHERE Correo='{test_email}';")
            assert stored.startswith('$2') and stored != registration['clave']
            request('POST','/api/auth/registro',409,registration,label='registro-409')
            request('POST','/api/auth/registro',409,dict(registration,correo=test_email.upper()))
            request('POST','/api/auth/registro',400,dict(registration,rol='Veterinario'),label='registro-rol-400')
            request('POST','/api/auth/registro',400,{})
            request('POST','/api/auth/registro',400,dict(registration,correo='no-es-correo'))
            request('POST','/api/auth/registro',400,dict(registration,clave='corta'))
            request('POST','/api/auth/registro',400,dict(registration,clave='ñ'*40))
            bad,_=request('POST','/api/auth/login',401,{'correo':registration['correo'],'clave':'Incorrecta2026!'},label='login-401')
            missing,_=request('POST','/api/auth/login',401,{'correo':'ausente-'+unique+'@veterinaria.test','clave':'Incorrecta2026!'})
            assert bad==missing
            request('POST','/api/auth/login',400,{})

            def parallel_register():
                req=Request(BASE+'/api/auth/registro',data=json.dumps({'correo':parallel_email,'clave':'PruebaSegura2026!'}).encode(),headers={'Content-Type':'application/json'},method='POST')
                try:
                    with urlopen(req,timeout=20) as r: return r.status
                except HTTPError as e: return e.code
            with ThreadPoolExecutor(max_workers=2) as executor:
                assert sorted(executor.map(lambda _:parallel_register(),range(2))) == [201,409]

            for method,path in [('GET','/api/auth/perfil'),('GET','/api/clientes'),('GET','/api/clientes/1'),('GET','/api/clientes/1/mascotas'),('POST','/api/clientes'),('PUT','/api/clientes/1'),('DELETE','/api/clientes/1'),('GET','/api/mascotas'),('GET','/api/mascotas/1'),('GET','/api/mascotas/buscar?especie=Perro'),('POST','/api/mascotas'),('PUT','/api/mascotas/1'),('DELETE','/api/mascotas/1')]:
                request(method,path,401,{} if method in ['POST','PUT'] else None,label='sin-token-401' if path=='/api/auth/perfil' else None)
            for role,resource in [('Recepcionista','mascotas'),('Veterinario','clientes')]:
                for method,path in [('POST',f'/api/{resource}'),('PUT',f'/api/{resource}/2147483647'),('DELETE',f'/api/{resource}/2147483647')]:
                    request(method,path,403,{} if method in ['POST','PUT'] else None,role=role,label=role.lower()+'-403' if method=='POST' else None)
            for role in ['Recepcionista','Veterinario']:
                for path in ['/api/clientes','/api/clientes/1','/api/clientes/1/mascotas','/api/mascotas','/api/mascotas/1','/api/mascotas/buscar?especie=Perro']:
                    request('GET',path,200,role=role)
                request('GET','/api/clientes/2147483647',404,role=role,label=role.lower()+'-404')

            claims=decode(TOKENS['Veterinario'])
            for mutated in [dict(claims,iss='EmisorIncorrecto'),dict(claims,aud='AudienciaIncorrecta'),dict(claims,exp=int(time.time())-60,nbf=int(time.time())-120),dict(claims,nbf=int(time.time())+3600,exp=int(time.time())+7200),{k:v for k,v in claims.items() if k!='rol'}]:
                request('GET','/api/auth/perfil',401,token=signed(mutated))
            request('GET','/api/auth/perfil',401,token=signed(claims,key='OtraClaveDePruebaSinRelacionConLaReal123456789'))
            parts=TOKENS['Recepcionista'].split('.')
            changed=dict(decode(TOKENS['Recepcionista']),rol='Veterinario')
            parts[1]=base64.urlsafe_b64encode(json.dumps(changed).encode()).decode().rstrip('=')
            request('POST','/api/mascotas',401,{},token='.'.join(parts),label='token-alterado-401')

            temp,_=request('POST','/api/auth/login',200,registration)
            sql(f"UPDATE dbo.Usuarios SET Rol='Veterinario' WHERE Correo='{test_email}';")
            profile,_=request('GET','/api/auth/perfil',200,token=temp['token'])
            assert profile['rol']=='Recepcionista'
            newer,_=request('POST','/api/auth/login',200,registration)
            assert decode(newer['token'])['rol']=='Veterinario'

            client={'nombre':'Prueba','apellido':'Integracion','dni':'87654321','telefono':'999888777','email':'prueba@example.com'}
            c,headers=request('POST','/api/clientes',201,client,role='Recepcionista',label='recepcionista-201')
            client_id=c['id']
            assert headers['Location'].endswith(f'/api/clientes/{client_id}')
            request('PUT',f'/api/clientes/{client_id}',204,dict(client,nombre='Actualizado'),role='Recepcionista',label='recepcionista-204')
            request('POST','/api/clientes',400,{},role='Recepcionista',label='recepcionista-400')
            request('PUT','/api/clientes/2147483647',404,client,role='Recepcionista')
            pets,_=request('GET',f'/api/clientes/{client_id}/mascotas',200,role='Recepcionista')
            assert pets==[]
            pet={'nombre':'TobyPrueba','especie':'Perro','raza':'Mestizo','edad':3,'clienteId':client_id}
            p,headers=request('POST','/api/mascotas',201,pet,role='Veterinario',label='veterinario-201')
            pet_id=p['id']
            assert p['nombreCliente']=='Actualizado Integracion' and 'cliente' not in p
            assert headers['Location'].endswith(f'/api/mascotas/{pet_id}')
            for role in ['Recepcionista','Veterinario']:
                p,_=request('GET',f'/api/mascotas/{pet_id}',200,role=role,label=role.lower()+'-200')
                assert p['id']==pet_id
                pets,_=request('GET',f'/api/clientes/{client_id}/mascotas',200,role=role)
                assert pets[0]['id']==pet_id
            request('DELETE',f'/api/clientes/{client_id}',409,role='Recepcionista',label='recepcionista-409')
            request('POST','/api/mascotas',409,dict(pet,edad=31),role='Veterinario',label='veterinario-409')
            request('POST','/api/mascotas',409,dict(pet,clienteId=2147483647),role='Veterinario')
            request('POST','/api/mascotas',400,dict(pet,edad=-1),role='Veterinario',label='veterinario-400')
            request('POST','/api/mascotas',400,{k:v for k,v in pet.items() if k!='edad'},role='Veterinario')
            request('PUT',f'/api/mascotas/{pet_id}',409,dict(pet,edad=31),role='Veterinario')
            request('PUT',f'/api/mascotas/{pet_id}',409,dict(pet,clienteId=2147483647),role='Veterinario')
            request('PUT',f'/api/mascotas/{pet_id}',400,{},role='Veterinario')
            request('PUT','/api/mascotas/2147483647',404,pet,role='Veterinario')
            request('PUT',f'/api/mascotas/{pet_id}',204,dict(pet,edad=30),role='Veterinario',label='veterinario-204')
            p,_=request('GET',f'/api/mascotas/{pet_id}',200,role='Veterinario')
            assert p['edad']==30
            other,_=request('POST','/api/clientes',201,dict(client,nombre='Otro'),role='Recepcionista')
            other_id=other['id']
            request('PUT',f'/api/mascotas/{pet_id}',204,dict(pet,clienteId=other_id,edad=0),role='Veterinario')
            p,_=request('GET',f'/api/mascotas/{pet_id}',200,role='Veterinario')
            assert p['nombreCliente']=='Otro Integracion' and p['edad']==0
            request('GET','/api/mascotas/buscar',400,role='Veterinario')
            empty,_=request('GET','/api/mascotas/buscar?especie=Inexistente',200,role='Recepcionista')
            assert empty==[]
            request('GET','/api/clientes/2147483647/mascotas',404,role='Recepcionista')
            request('DELETE',f'/api/mascotas/{pet_id}',204,role='Veterinario',label='veterinario-delete-204')
            request('DELETE',f'/api/mascotas/{pet_id}',404,role='Veterinario')
            pet_id=None
            request('DELETE',f'/api/clientes/{client_id}',204,role='Recepcionista',label='recepcionista-delete-204')
            request('DELETE',f'/api/clientes/{client_id}',404,role='Recepcionista')
            client_id=None
            request('DELETE',f'/api/clientes/{other_id}',204,role='Recepcionista')
            other_id=None
            assert set(r['estado'] for r in CHECKS)=={200,201,204,400,401,403,404,409}
            reports()
            print(f'\n{len(CHECKS)} solicitudes verificadas; concurrencia [201,409]; 15 operaciones; 8 códigos HTTP.')
        finally:
            for resource,identity,role in [('mascotas',pet_id,'Veterinario'),('clientes',client_id,'Recepcionista'),('clientes',other_id,'Recepcionista')]:
                if identity:
                    try: request('DELETE',f'/api/{resource}/{identity}',204,role=role)
                    except Exception as error: print('Limpieza pendiente:',resource,identity,error)
            sql(f"DELETE FROM dbo.Usuarios WHERE Correo IN ('{test_email}','{parallel_email}');")
            process.terminate()
            process.wait(timeout=15)

if __name__=='__main__':
    main()

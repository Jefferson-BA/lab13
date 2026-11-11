"""Prueba de integración real: levanta la API, usa SQL Server y limpia sus registros."""
import json
import os
from pathlib import Path
import subprocess
import time
from urllib.request import Request, urlopen
from urllib.error import HTTPError, URLError

ROOT = Path(__file__).resolve().parents[1]
BASE = 'http://localhost:5131'
checks = []

def request(method, path, expected, payload=None):
    data = None if payload is None else json.dumps(payload).encode('utf-8')
    req = Request(BASE + path, data=data, method=method, headers={'Content-Type': 'application/json'})
    try:
        response = urlopen(req, timeout=15)
    except HTTPError as error:
        response = error
    raw = response.read().decode('utf-8')
    assert response.status == expected, f'{method} {path}: esperaba {expected}, recibí {response.status}: {raw}'
    body = json.loads(raw) if raw else None
    checks.append({'metodo': method, 'ruta': path, 'estado': response.status})
    print(f'OK {expected} {method} {path}')
    return body, response.headers

if __name__ == '__main__':
    output = ROOT / 'artifacts'
    output.mkdir(exist_ok=True)
    env = dict(os.environ, ASPNETCORE_ENVIRONMENT='Development', ASPNETCORE_URLS=BASE)
    client_id = pet_id = other_id = None
    with (output / 'api-pruebas.log').open('w', encoding='utf-8') as log:
        process = subprocess.Popen(['dotnet', str(ROOT / 'VeterinariaApp.Api/bin/Debug/net10.0/VeterinariaApp.Api.dll')],
                                   cwd=ROOT / 'VeterinariaApp.Api', env=env, stdout=log, stderr=log)
        try:
            for _ in range(60):
                if process.poll() is not None:
                    raise RuntimeError('La API no inició. Ver artifacts/api-pruebas.log')
                try:
                    with urlopen(BASE + '/swagger/v1/swagger.json', timeout=1) as r:
                        spec = json.load(r)
                    break
                except (URLError, TimeoutError):
                    time.sleep(0.5)
            else:
                raise TimeoutError('La API no respondió.')
            assert sum(len([m for m in methods if m in ['get','post','put','delete']]) for methods in spec['paths'].values()) == 12
            request('GET', '/api/clientes', 200)
            request('GET', '/api/mascotas', 200)
            for path in ['/api/clientes/2147483647', '/api/mascotas/2147483647', '/api/clientes/2147483647/mascotas']:
                request('GET', path, 404)
            client = {'nombre': 'Prueba', 'apellido': 'Integracion', 'dni': '87654321', 'telefono': '999888777', 'email': 'prueba@example.com'}
            c, headers = request('POST', '/api/clientes', 201, client)
            client_id = c['id']
            assert headers['Location'].endswith(f'/api/clientes/{client_id}')
            request('GET', f'/api/clientes/{client_id}', 200)
            pets, _ = request('GET', f'/api/clientes/{client_id}/mascotas', 200)
            assert pets == []
            client['nombre'] = 'Actualizado'
            request('PUT', f'/api/clientes/{client_id}', 204, client)
            c, _ = request('GET', f'/api/clientes/{client_id}', 200)
            assert c['nombre'] == 'Actualizado'
            request('POST', '/api/clientes', 400, {})
            request('POST', '/api/clientes', 400, dict(client, dni='123', email='invalido'))
            request('PUT', f'/api/clientes/{client_id}', 400, {})
            request('PUT', '/api/clientes/2147483647', 404, client)
            pet = {'nombre': 'TestPet', 'especie': 'Perro', 'raza': 'Mestizo', 'edad': 3, 'clienteId': client_id}
            p, headers = request('POST', '/api/mascotas', 201, pet)
            pet_id = p['id']
            assert p['nombreCliente'] == 'Actualizado Integracion'
            assert 'cliente' not in p
            assert headers['Location'].endswith(f'/api/mascotas/{pet_id}')
            request('GET', f'/api/mascotas/{pet_id}', 200)
            pets, _ = request('GET', f'/api/clientes/{client_id}/mascotas', 200)
            assert len(pets) == 1 and pets[0]['id'] == pet_id
            pets, _ = request('GET', '/api/mascotas/buscar?especie=Perro', 200)
            assert any(p['id'] == pet_id for p in pets)
            assert all(p['especie'] == 'Perro' for p in pets)
            pets, _ = request('GET', '/api/mascotas/buscar?especie=Inexistente', 200)
            assert pets == []
            request('GET', '/api/mascotas/buscar', 400)
            request('DELETE', f'/api/clientes/{client_id}', 409)
            request('POST', '/api/mascotas', 409, dict(pet, edad=31))
            request('POST', '/api/mascotas', 409, dict(pet, clienteId=2147483647))
            request('POST', '/api/mascotas', 400, dict(pet, edad=-1))
            request('POST', '/api/mascotas', 400, {'nombre': 'Faltan campos'})
            request('POST', '/api/mascotas', 400, {k:v for k,v in pet.items() if k != 'edad'})
            request('PUT', f'/api/mascotas/{pet_id}', 409, dict(pet, edad=31))
            request('PUT', f'/api/mascotas/{pet_id}', 409, dict(pet, clienteId=2147483647))
            request('PUT', f'/api/mascotas/{pet_id}', 400, {})
            request('PUT', '/api/mascotas/2147483647', 404, pet)
            pet['edad'] = 30
            request('PUT', f'/api/mascotas/{pet_id}', 204, pet)
            p, _ = request('GET', f'/api/mascotas/{pet_id}', 200)
            assert p['edad'] == 30
            other, _ = request('POST', '/api/clientes', 201, dict(client, nombre='Otro'))
            other_id = other['id']
            pet['clienteId'] = other_id
            request('PUT', f'/api/mascotas/{pet_id}', 204, pet)
            p, _ = request('GET', f'/api/mascotas/{pet_id}', 200)
            assert p['nombreCliente'] == 'Otro Integracion'
            request('DELETE', f'/api/mascotas/{pet_id}', 204)
            request('DELETE', f'/api/mascotas/{pet_id}', 404)
            pet_id = None
            request('DELETE', f'/api/clientes/{client_id}', 204)
            request('DELETE', f'/api/clientes/{client_id}', 404)
            client_id = None
            request('DELETE', f'/api/clientes/{other_id}', 204)
            other_id = None
            (output / 'resultado-pruebas.json').write_text(json.dumps({'resultado': 'Correcto', 'total': len(checks), 'pruebas': checks}, indent=2), encoding='utf-8')
            print(f'\n{len(checks)} solicitudes verificadas; 12 rutas documentadas en Swagger.')
        finally:
            for entity, identity in [('mascotas', pet_id), ('clientes', client_id), ('clientes', other_id)]:
                if identity:
                    try:
                        request('DELETE', f'/api/{entity}/{identity}', 204)
                    except Exception as error:
                        print('Limpieza pendiente:', entity, identity, error)
            process.terminate()
            process.wait(timeout=15)

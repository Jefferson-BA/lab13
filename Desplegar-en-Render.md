# Desplegar la API y el gateway en Render

Citas ya existe en https://veterinaria-citas.onrender.com. Neon ya tiene el esquema PostgreSQL. Este procedimiento publica los otros dos servicios desde el repositorio Jefferson-BA/lab13.

## Publicar la API

1. Hacer commit y push de los cambios actuales a main. No incluir .env.local.
2. En Render seleccionar New > Web Service y el repositorio lab13.
3. Nombre veterinaria-api, Language Docker, Branch main y región Oregon.
4. Root Directory vacío. En Advanced: Dockerfile Path ./Dockerfile, Docker Build Context . y Docker Command vacío.
5. Seleccionar Free ($0). Health Check Path: /health.
6. En Environment > Add from .env copiar despliegue/api.env.example.
7. Sustituir Jwt__Clave por la misma clave que usa Citas.
8. Sustituir ConnectionStrings__VeterinariaDB por el valor privado de .env.local, sin el nombre de la variable y sin comillas exteriores. Es formato Host=...;Database=...;Username=...;Password=..., no una URL postgresql://.
9. Deploy web service. Guardar la URL real asignada, que puede incluir un sufijo.
10. Consultar URL-API/health y URL-API/api/clientes: ambos deben responder 200. La lista puede estar vacía porque los registros de LocalDB no se copiaron a Neon.

La API tiene nueve variables. No ejecutar la inicialización de tablas como Start Command ni Pre-Deploy Command: el esquema de Neon ya está preparado. El Dockerfile inicia la aplicación.

## Publicar el gateway

1. Crear otro Web Service con nombre veterinaria-gateway, el mismo repositorio y configuración Docker, main, Oregon, Free y raíz vacía.
2. Configurar Health Check Path: /health.
3. Importar despliegue/gateway.env.example y completar los valores marcados.
4. Jwt__Clave debe coincidir con la API y Citas. Servicios__ApiUrl debe ser la URL HTTPS real de la API, sin /api ni otras rutas. Servicios__CitasUrl ya contiene la URL publicada de Citas.
5. GlobalConfiguration__BaseUrl debe ser la URL HTTPS real del gateway. Si Render asigna otro subdominio, corregirla en Environment y volver a desplegar.
6. Deploy web service. No agregar ninguna conexión de Neon a este servicio.

El gateway tiene nueve variables. Servicios__ApiUrl y Servicios__CitasUrl sustituyen los destinos de las siete rutas de ocelot.json en memoria al arrancar. No se necesitan las 21 variables Routes__... si se usan estas dos URL.

Si se utiliza scripts/Configurar-RutasNube.ps1 para generar ocelot.Production.json con las direcciones reales, el archivo se puede capturar para la entrega del laboratorio. Las URL de Servicios__... tienen prioridad sobre los destinos de ese archivo.

## Comprobar la publicación

- URL-GATEWAY/health debe responder 200. /health comprueba el arranque del proceso; no comprueba la disponibilidad de Neon ni de los microservicios.
- URL-GATEWAY/gateway/clientes y /gateway/mascotas deben responder 200.
- URL-GATEWAY/gateway/citas sin token debe responder 401.
- POST /gateway/auth/registro y POST /gateway/auth/login permiten obtener el token. El registro asigna Recepcionista.
- Con token, GET /gateway/citas debe responder 200; POST /gateway/mascotas con Recepcionista debe responder 403.
- Ejecutar el script de pruebas contra las URL reales, con los tres servicios iniciados:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Probar-Gateway.ps1 -BaseUrl https://TU-GATEWAY.onrender.com -CitasDirecto https://veterinaria-citas.onrender.com
```

El script registra una cuenta de prueba y elimina el cliente temporal. La opción -CitasDetenidas exige 502. En Render, suspender Citas puede hacer que la plataforma responda 503: registrar el código real, no presentarlo como 502. Para demostrar además 502 se puede configurar temporalmente Servicios__CitasUrl=https://citas-detenidas.invalid, volver a desplegar el gateway y ejecutar con -CitasDetenidas; esta prueba simula un destino inaccesible y no equivale a detener el proceso. Restaurar siempre la URL real y reanudar Citas. Las primeras llamadas pueden demorarse por el arranque del plan gratuito; abrir primero las URL y esperar antes de ejecutar la suite.

## Alternativa Blueprint

render.yaml permite crear solo API y gateway mediante New > Blueprint. No combinar ambos procedimientos para crear los mismos servicios. Los dos planes están fijados en free; los valores privados se solicitan al crear el Blueprint. Introducir la misma clave JWT de Citas en ambos servicios. Completar las URL reales cuando Render las asigne.

La configuración está preparada; guardar estos archivos no crea ni publica servicios por sí solo. Sigue pendiente hacer commit/push de estos cambios y realizar el despliegue en la cuenta de Render.

Referencias: [Docker en Render](https://render.com/docs/docker), [Blueprints y variables privadas](https://render.com/docs/blueprint-spec).

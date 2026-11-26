# Laboratorios 13 14 y 15 API de gestión veterinaria

## Semana 15 Gateway y microservicio Citas

El despliegue actual en Render se describe en [despliegue/README.md](despliegue/README.md). Esa carpeta incluye plantillas separadas de variables para API y gateway. render.yaml es una alternativa opcional para crear ambos mediante Blueprint con plan gratuito. Citas ya esta publicado y no se vuelve a crear.

Para el gateway se recomiendan Servicios__ApiUrl y Servicios__CitasUrl con los origenes HTTPS publicos reales. Estas variables actualizan los destinos de Ocelot al iniciar y evitan introducir 21 variables por ruta. API y gateway exponen /health para comprobar el arranque; la prueba completa de base de datos se realiza consultando clientes o mascotas.

### Variables en un archivo local

Copiar la plantilla desde la raíz, sin sobrescribir un archivo privado existente:

```powershell
if (-not (Test-Path .env.local)) { Copy-Item .env.example .env.local }
```

Completar Jwt__Clave con una clave aleatoria y ConnectionStrings__VeterinariaDB con los datos de Neon en formato Npgsql. La plantilla usa TLS con verificación de certificado y nombre de servidor. Establecer BaseDatos__Proveedor=PostgreSQL para Neon. Si se omite, la API conserva SqlServer para la configuración histórica con LocalDB.

La base histórica VeterinariaDB está en SQL Server LocalDB en este equipo, no en MySQL ni en Render. Neon es otra base; no se transfieren automáticamente los registros de LocalDB. El esquema PostgreSQL está en sql/Neon-S15.sql, sin cuentas ni contraseñas de muestra. Inicializarlo una vez (también admite repetición sin borrar datos):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Iniciar-ConEnv.ps1 -Proyecto VeterinariaApp.Api -InicializarBaseDatos
```

El comando explícito --inicializar-bd crea Clientes, Mascotas, Usuarios, índices y restricciones y termina. El arranque normal de la API no modifica el esquema. Para el cambio de proveedor se adaptaron los identificadores SQL de las restricciones y la traducción del error de correo duplicado. Los roles y reglas de negocio se conservan.

ASP.NET Core no carga .env.local automáticamente. Para importar las variables correspondientes a cada servicio y ejecutarlo, usar una terminal por proyecto:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Iniciar-ConEnv.ps1 -Proyecto GatewayApi
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Iniciar-ConEnv.ps1 -Proyecto VeterinariaApp.Api
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Iniciar-ConEnv.ps1 -Proyecto VeterinariaApp.Citas
```

El script acepta líneas NOMBRE=valor, comentarios de línea completa y comillas exteriores simples o dobles. No ejecuta expresiones ni expande variables. Las variables se limitan al proceso iniciado; la conexión de base de datos se importa solo para la API. No iniciar otra copia sobre los mismos puertos si los servicios ya están ejecutándose.

.env.local está excluido de Git y del contexto Docker. En Render configurar Environment por servicio usando las secciones de .env.example; establecer ASPNETCORE_ENVIRONMENT=Production y ASPNETCORE_HTTP_PORTS=8080. Los valores se ingresan sin las comillas exteriores del archivo. El argumento Docker PROJECT se configura por separado al construir cada imagen.

Referencias: [variables de entorno de .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers), [seguridad de Npgsql](https://www.npgsql.org/doc/security.html).

La solución ahora incluye GatewayApi y VeterinariaApp.Citas. Los GET de clientes y mascotas son públicos para cumplir la S15; la escritura conserva los permisos por rol de la S14. El perfil y todas las citas requieren JWT. No se comparten clases ni base de datos con Citas.

Puertos locales: API 5130, Gateway 5143 y Citas 5020. El cliente usa http://localhost:5143/gateway. Swagger de la API sigue disponible en http://localhost:5130/swagger para inspección.

Si los secretos de la API ya están configurados, sincronizar el JWT sin cambiar su clave:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Configurar-GatewayLocal.ps1
dotnet build lab13.slnx
```

Si se vuelve a ejecutar Configurar-Local.ps1, repetir la sincronización y reiniciar los tres servicios.

Ejecutar cada comando en una terminal distinta:

```powershell
dotnet run --project VeterinariaApp.Api --launch-profile http
dotnet run --project GatewayApi --launch-profile http
dotnet run --project VeterinariaApp.Citas --launch-profile http
```

En Visual Studio también se puede usar Solución → Configurar proyectos de inicio → Varios proyectos y establecer Iniciar para esos tres proyectos, dejando las bibliotecas sin acción.

Las siete rutas están en GatewayApi/ocelot.json:

- POST /gateway/auth/{everything} → /api/auth/{everything}, público para login y registro.
- GET /gateway/auth/perfil → /api/auth/perfil, requiere token.
- GET /gateway/clientes/{everything} → /api/clientes/{everything}, público.
- POST PUT DELETE /gateway/clientes/{everything} → /api/clientes/{everything}, requiere token; Recepcionista en la API.
- GET /gateway/mascotas/{everything} → /api/mascotas/{everything}, público.
- POST PUT DELETE /gateway/mascotas/{everything} → /api/mascotas/{everything}, requiere token; Veterinario en la API.
- GET /gateway/citas/{everything} → /api/citas/{everything}, requiere token; lista o por ID, en memoria.

El comodín también admite la lista sin ID. Ocelot valida Bearer y reenvía el token; cada servicio lo valida nuevamente con la misma clave, emisor y audiencia. Los roles los controla la API original.

Pruebas contra servicios ya iniciados:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Probar-Gateway.ps1
# Detener solo Citas y ejecutar:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Probar-Gateway.ps1 -CitasDetenidas
# Volver a iniciar Citas al terminar.
```

Se comprobaron localmente 200, 201, 204, 401, 403, 404 y 502. El script crea una cuenta Recepcionista única por ejecución y elimina el cliente temporal. No imprime claves ni tokens. La prueba con Citas detenido confirma 502 y que clientes sigue funcionando. Las pruebas de S14 reflejan los permisos anteriores y no sustituyen estas pruebas de S15.

### Publicación pendiente

Dockerfile permite publicar cada servicio usando el argumento PROJECT (GatewayApi, VeterinariaApp.Api o VeterinariaApp.Citas). Se construye desde la raíz del repositorio y escucha HTTP en 8080 dentro del contenedor.

```powershell
docker build --build-arg PROJECT=GatewayApi -t veterinaria-gateway .
docker build --build-arg PROJECT=VeterinariaApp.Api -t veterinaria-api .
docker build --build-arg PROJECT=VeterinariaApp.Citas -t veterinaria-citas .
```

Configurar Jwt__Clave, Jwt__Emisor y Jwt__Audiencia iguales en los tres servicios mediante secretos del proveedor. Solo la API necesita ConnectionStrings__VeterinariaDB y Jwt__MinutosExpiracion. Ejecutar los scripts SQL en la base de nube sin borrar datos existentes. Los secretos locales de .NET no se copian a las imágenes.

Cuando existan las tres URL públicas HTTPS, ejecutar Configurar-RutasNube.ps1. Genera GatewayApi/ocelot.Production.json con los hosts reales. El gateway lo carga en Production, y las variables de entorno pueden sobrescribirlo.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Configurar-RutasNube.ps1 -Gateway https://HOST-GATEWAY -Api https://HOST-API -Citas https://HOST-CITAS
```

Citas está publicado en https://veterinaria-citas.onrender.com; requiere JWT en /api/citas. La API local ya fue comprobada contra PostgreSQL de Neon. Quedan pendientes publicar la API y el gateway en Render y repetir las pruebas públicas y las capturas. Revisar las condiciones de AutoMapper antes de usar producción.

En el Web Service de la API, usar Docker, plan Free, raíz vacía y estas nueve variables: PROJECT=VeterinariaApp.Api, ASPNETCORE_ENVIRONMENT=Production, ASPNETCORE_HTTP_PORTS=8080, BaseDatos__Proveedor=PostgreSQL, Jwt__Emisor=VeterinariaApp.Api, Jwt__Audiencia=VeterinariaApp.Clientes, Jwt__Clave (igual a Citas), Jwt__MinutosExpiracion=60 y ConnectionStrings__VeterinariaDB (cadena Npgsql privada). No agregar la conexión de base de datos a los otros servicios. En Render copiar el valor de conexión sin las comillas exteriores del archivo .env.local.

Para el video obligatorio de al menos 10 minutos, con cámara encendida: explicar los tres servicios, upstream y downstream, la validación JWT duplicada y su costo, los roles, las pruebas públicas y la caída independiente de Citas. El gateway simplifica la dirección usada por el cliente, pero agrega otro componente que debe configurarse y mantenerse.

Implementación en C# y ASP.NET Core .NET 10 con SQL Server, Entity Framework Core, DTOs y AutoMapper. La semana 14 agrega registro, login, perfil y permisos JWT a las doce operaciones de clientes y mascotas, conservando los cuatro proyectos.

La entrega de la semana 14 está en [docs/Laboratorio14.md](docs/Laboratorio14.md), con tema, roles, matriz de permisos, pruebas y sustentación de seguridad.

## Ejecutar en este equipo

Requisitos: SDK de .NET 10, SQL Server LocalDB y `sqlcmd`. Los tres están disponibles en el equipo en el que se implementó este laboratorio.

Desde esta carpeta, en PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Configurar-Local.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\Inicializar-BaseDatos.ps1
dotnet build .\lab13.slnx
dotnet run --project .\VeterinariaApp.Api --launch-profile http
```

Abrir <http://localhost:5130/swagger>. En Visual Studio, abrir `lab13.slnx`, establecer `VeterinariaApp.Api` como proyecto de inicio y ejecutar el perfil `http`.

La base fue creada y cargada con 3 clientes, 5 mascotas y 2 usuarios de ejemplo, uno por rol. Las cuentas son `recepcion@veterinaria.test` y `veterinario@veterinaria.test`; su contraseña exclusiva de laboratorio es `Veterinaria2026!`. El registro nuevo siempre asigna Recepcionista. Para probar las rutas protegidas, iniciar sesión en `/api/auth/login`, pulsar Authorize en Swagger y pegar solo el token.

El script `Configurar-Local.ps1` guarda la conexión, la clave JWT aleatoria y la duración en secretos locales de .NET, fuera del repositorio. `appsettings.json` conserva únicamente los nombres de configuración y valores vacíos o cero para esos campos. Ejecutar nuevamente el script cambia la clave e invalida tokens anteriores.

Repetir el inicializador SQL no elimina datos ni vuelve a insertar las muestras si ya hay clientes. No se crean tablas automáticamente al iniciar la API: el script SQL es el punto de partida del modelo de base de datos.

Si se usa otra instancia de SQL Server, indicar `-Servidor` al ejecutar ambos scripts. La conexión privada usa autenticación de Windows y se guarda en secretos locales de .NET. También puede configurarse mediante `ConnectionStrings__VeterinariaDB`. La confianza en el certificado corresponde únicamente al entorno local del laboratorio.

## Tema y dominio

Una veterinaria necesita registrar los datos de contacto de sus clientes y consultar las mascotas de cada propietario. `Cliente` contiene Id, Nombre, Apellido, Dni, Telefono y Email. `Mascota` contiene Id, Nombre, Especie, Raza, Edad y ClienteId.

La relación es uno a muchos: un cliente puede tener cero o muchas mascotas y cada mascota pertenece a un cliente. `Mascota.ClienteId` es la clave foránea; las propiedades de navegación son `Cliente.Mascotas` y `Mascota.Cliente`.

Reglas de negocio:

1. No se elimina un cliente mientras tenga mascotas registradas. Como el dominio especificado no incluye un campo `Activo`, toda mascota registrada se considera activa. Primero se eliminan o transfieren sus mascotas.
2. No se crea ni actualiza una mascota con más de 30 años. Tampoco se permite asignarla a un cliente inexistente.

Las reglas se implementan en los servicios y lanzan `ReglaNegocioException`. Los controladores capturan esa excepción y generan `409 Conflict` con un cuerpo ProblemDetails. Las restricciones de edad y clave foránea de SQL refuerzan la integridad de los datos.

## Arquitectura y código

Las referencias directas entre proyectos siguen esta cadena:

```text
VeterinariaApp.Api → VeterinariaApp.Negocio → VeterinariaApp.Datos → VeterinariaApp.Entidades
```

- `VeterinariaApp.Entidades`: modelos Cliente, Mascota y Usuario; DTOs de clientes, mascotas y autenticación en `DTOs/`; constantes de Roles.
- `VeterinariaApp.Datos`: `VeterinariaContext` y tres interfaces con sus repositorios en `Repositorios/`.
- `VeterinariaApp.Negocio`: excepción, `MappingProfile` y tres interfaces con sus servicios en `Servicios/`.
- `VeterinariaApp.Api`: tres controladores y configuración de servicios, EF, AutoMapper, autenticación JWT y Swagger en `Program.cs`.

El código completo está en esos proyectos. Se reemplazó la plantilla WeatherForecast. Los controladores reciben únicamente interfaces de servicios y DTOs. `Program.cs` es el punto de composición y registra las implementaciones de las distintas capas. Las referencias de .NET son transitivas: el proyecto API puede ver los DTOs sin agregar una referencia directa adicional.

Los repositorios ejecutan las consultas LINQ con `ToListAsync`, `FirstOrDefaultAsync` y `AnyAsync`; las escrituras usan `SaveChangesAsync`. Las inserciones y eliminaciones usan las operaciones de seguimiento de EF, ya que LINQ se emplea para consultas. Se propaga `CancellationToken` en todas las operaciones. Las consultas de mascotas cargan `Cliente` mediante `Include`, para que AutoMapper pueda obtener el nombre completo sin consultas adicionales por cada mascota.

Las listas usan `AsNoTracking`; las búsquedas individuales permiten actualizar la entidad seguida. No se serializan entidades ni se usa `[JsonIgnore]`. `PUT` representa el reemplazo de los campos editables y conserva el Id de la ruta.

## Validaciones y códigos HTTP

Los DTOs de entrada utilizan Required, StringLength, EmailAddress, Phone, RegularExpression y Range según el campo. El DNI requiere exactamente ocho dígitos. El Id del cliente debe ser positivo y la edad no puede ser negativa. Edad y ClienteId son enteros anulables en la entrada para que Required detecte su ausencia en el JSON, incluso cuando cero es una edad válida.

`[ApiController]` devuelve `400` al fallar estas validaciones. El máximo de 30 años se comprueba en el servicio, por lo que enviar 31 devuelve `409`, en lugar de quedar interceptado como `400` por Range. Los DTOs de lectura mantienen enteros no anulables.

Las consultas existentes devuelven `200`; POST devuelve `201` con Location y el DTO creado; PUT y DELETE correctos devuelven `204` sin cuerpo. Un recurso inexistente devuelve `404`. Una lista vacía o una búsqueda sin coincidencias devuelve `200` y `[]`. La ruta anidada devuelve `404` si el cliente no existe y `200` con `[]` si existe sin mascotas.

## Pruebas manuales y automáticas

Abrir `VeterinariaApp.Api/VeterinariaApp.Api.http` con Visual Studio o un cliente HTTP compatible. Incluye las quince operaciones y casos adicionales de error. También puede importarse la colección `pruebas/Veterinaria-S14.postman_collection.json`. En Swagger, pulsar Try it out, completar los datos y ejecutar.

Las consultas requieren autenticación. Para modificar clientes, usar el token Recepcionista; para modificar mascotas, usar Veterinario. Los ejemplos de códigos de esta sección requieren esos permisos. El archivo HTTP explica dónde pegar cada token.

Para los ejemplos, usar un cliente y una mascota creados durante la prueba. Los Id son autoincrementales y pueden tener saltos: copiar el Id devuelto por POST en `clienteId` y `mascotaId` del archivo `.http`. `2147483647` se reserva en los ejemplos como un Id que no existe en la base de laboratorio.

Para forzar cada código:

1. `200`: consultar clientes, una mascota existente, la ruta anidada o el filtro por especie.
2. `201`: crear un cliente válido y después una mascota para ese cliente. Verificar el encabezado Location.
3. `204`: actualizar un registro existente; eliminar primero su mascota y después el cliente.
4. `400`: enviar `{}`; un correo inválido; un DNI de tres dígitos; edad negativa; o no enviar edad. La búsqueda sin `especie` también devuelve 400.
5. `404`: consultar, actualizar o eliminar un Id inexistente. Para actualizar, enviar un cuerpo válido para que la validación no responda 400 antes de buscar el registro.
6. `409`: intentar eliminar un cliente con mascotas; crear o actualizar una mascota de 31 años; o asignarla a un cliente positivo inexistente.

La prueba automática usa SQL Server real y un proceso de API en el puerto 5131:

```powershell
dotnet build .\lab13.slnx
python .\scripts\probar_api.py
```

No requiere paquetes de Python externos. Crea registros propios, los limpia al finalizar y detiene el proceso que inició. Verifica las quince operaciones, los ocho códigos, ambos roles, la firma y vigencia JWT, BCrypt, correo único concurrente y las reglas de S13. Guarda el resultado JSON, el log y las evidencias HTML en `artifacts/s14/`. No modificar simultáneamente sus registros durante la ejecución.

## Reconstrucción y Database First

Los comandos exactos para crear los cuatro proyectos y agregar los paquetes están en `docs/Comandos.md`. `scripts/Scaffold.ps1` ejecuta ingeniería inversa hacia `artifacts/scaffold/`, sin sobrescribir el código implementado. La separación de entidades en otro proyecto requiere trasladar los archivos generados y ajustar sus namespaces; los DTOs, servicios y reglas se escriben por separado.

La tabla de sustentación de los siete CreateMap está en [docs/Mapeos.md](docs/Mapeos.md). Las instrucciones de reconstrucción están en [docs/Comandos.md](docs/Comandos.md).

## Observaciones técnicas

1. La separación de responsabilidades permite cambiar las consultas del repositorio sin introducir SQL ni EF en los controladores. Las reglas se conservan en los servicios.
2. Los DTOs de entrada restringen los campos modificables; los de lectura publican una estructura estable sin navegación circular ni exposición de entidades completas.
3. La FK con eliminación restringida protege la relación en SQL. La comprobación del servicio produce un mensaje comprensible en el flujo normal; no equivale a una transacción que impida todas las carreras entre peticiones concurrentes.

## Conclusiones

1. AutoMapper concentra la conversión entre modelos y contratos. Validar el perfil al inicio detecta miembros de destino sin configuración antes de atender peticiones.
2. El mapeo explícito de NombreCliente permite enriquecer la respuesta sin devolver el objeto Cliente. Los mapeos de entrada ignoran Id y navegaciones para preservar el control del servidor.
3. Capturar ReglaNegocioException en los controladores mantiene las reglas independientes del protocolo HTTP. Este manejo descentralizado cumple el laboratorio; en una API mayor puede centralizarse con un filtro o manejador de excepciones para reducir repetición.

## Versiones y referencias

Se utilizan EF Core SQL Server, Design y Tools 10.0.12; AutoMapper 16.3.0, Swashbuckle.AspNetCore 11.0.0, BCrypt.Net-Next 4.2.0, System.IdentityModel.Tokens.Jwt 8.23.0 y JwtBearer 10.0.15. Se agregó Pkcs 10.0.13 para corregir las advertencias de vulnerabilidad de la dependencia transitiva anterior. El proyecto sigue dirigido a net10.0.

AutoMapper permite configurar su clave con `AutoMapper__LicenseKey` o secretos de usuario; no se incluye una clave en el repositorio. Consultar las condiciones aplicables y la [configuración oficial de licencia](https://docs.automapper.io/en/latest/License-configuration.html). El arranque sin clave utilizado en las pruebas emite la advertencia de licencia de la biblioteca.

Consultar también [ingeniería inversa de EF Core](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding) y el [aviso de Microsoft sobre Pkcs](https://github.com/advisories/GHSA-846f-f764-76mx).
"# lab13" 

# Laboratorio 13 API de gestión veterinaria

Implementación en C# y ASP.NET Core .NET 10 con SQL Server, Entity Framework Core, DTOs y AutoMapper. Permite administrar clientes y sus mascotas mediante doce operaciones REST asíncronas.

## Ejecutar en este equipo

Requisitos: SDK de .NET 10, SQL Server LocalDB y `sqlcmd`. Los tres están disponibles en el equipo en el que se implementó este laboratorio.

Desde esta carpeta, en PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Inicializar-BaseDatos.ps1
dotnet build .\lab13.slnx
dotnet run --project .\VeterinariaApp.Api --launch-profile http
```

Abrir <http://localhost:5130/swagger>. En Visual Studio, abrir `lab13.slnx`, establecer `VeterinariaApp.Api` como proyecto de inicio y ejecutar el perfil `http`.

La base ya fue creada y cargada con 3 clientes y 5 mascotas. Repetir el script no elimina datos ni vuelve a insertar las muestras si ya hay clientes. No se crean tablas automáticamente al iniciar la API: el script SQL es el punto de partida del modelo de base de datos.

Si se usa otra instancia de SQL Server, ejecutar el SQL en ella y ajustar `ConnectionStrings:VeterinariaDB` en `VeterinariaApp.Api/appsettings.json`. También puede sobrescribirse con la variable `ConnectionStrings__VeterinariaDB`. La cadena incluida usa autenticación de Windows y LocalDB. `TrustServerCertificate=True` corresponde al entorno local del laboratorio.

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

- `VeterinariaApp.Entidades`: modelos `Cliente` y `Mascota`; seis DTOs en `DTOs/`.
- `VeterinariaApp.Datos`: `VeterinariaContext` y dos interfaces con sus repositorios en `Repositorios/`.
- `VeterinariaApp.Negocio`: excepción, `MappingProfile` y dos interfaces con sus servicios en `Servicios/`.
- `VeterinariaApp.Api`: dos controladores y configuración de servicios, EF, AutoMapper y Swagger en `Program.cs`.

El código completo está en esos proyectos. Se reemplazó la plantilla WeatherForecast. Los controladores reciben únicamente interfaces de servicios y DTOs. `Program.cs` es el punto de composición y registra las implementaciones de las distintas capas. Las referencias de .NET son transitivas: el proyecto API puede ver los DTOs sin agregar una referencia directa adicional.

Los repositorios ejecutan las consultas LINQ con `ToListAsync`, `FirstOrDefaultAsync` y `AnyAsync`; las escrituras usan `SaveChangesAsync`. Las inserciones y eliminaciones usan las operaciones de seguimiento de EF, ya que LINQ se emplea para consultas. Se propaga `CancellationToken` en todas las operaciones. Las consultas de mascotas cargan `Cliente` mediante `Include`, para que AutoMapper pueda obtener el nombre completo sin consultas adicionales por cada mascota.

Las listas usan `AsNoTracking`; las búsquedas individuales permiten actualizar la entidad seguida. No se serializan entidades ni se usa `[JsonIgnore]`. `PUT` representa el reemplazo de los campos editables y conserva el Id de la ruta.

## Validaciones y códigos HTTP

Los DTOs de entrada utilizan Required, StringLength, EmailAddress, Phone, RegularExpression y Range según el campo. El DNI requiere exactamente ocho dígitos. El Id del cliente debe ser positivo y la edad no puede ser negativa. Edad y ClienteId son enteros anulables en la entrada para que Required detecte su ausencia en el JSON, incluso cuando cero es una edad válida.

`[ApiController]` devuelve `400` al fallar estas validaciones. El máximo de 30 años se comprueba en el servicio, por lo que enviar 31 devuelve `409`, en lugar de quedar interceptado como `400` por Range. Los DTOs de lectura mantienen enteros no anulables.

Las consultas existentes devuelven `200`; POST devuelve `201` con Location y el DTO creado; PUT y DELETE correctos devuelven `204` sin cuerpo. Un recurso inexistente devuelve `404`. Una lista vacía o una búsqueda sin coincidencias devuelve `200` y `[]`. La ruta anidada devuelve `404` si el cliente no existe y `200` con `[]` si existe sin mascotas.

## Pruebas manuales y automáticas

Abrir `VeterinariaApp.Api/VeterinariaApp.Api.http` con Visual Studio o un cliente HTTP compatible. Incluye las doce operaciones y casos adicionales de error. Para Postman, usar las mismas URL, método, JSON y encabezado `Content-Type: application/json`. En Swagger, pulsar Try it out, completar los datos y ejecutar.

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

No requiere paquetes de Python externos. Crea registros propios, los limpia al finalizar y detiene el proceso que inició. Verifica las doce rutas, los seis códigos, el encabezado Location, los campos mapeados, la actualización de propietario y el límite de 30 años. Guarda el resultado JSON y el log en `artifacts/`. No modificar simultáneamente sus registros durante la ejecución.

## Reconstrucción y Database First

Los comandos exactos para crear los cuatro proyectos y agregar los paquetes están en `docs/Comandos.md`. `scripts/Scaffold.ps1` ejecuta ingeniería inversa hacia `artifacts/scaffold/`, sin sobrescribir el código implementado. La separación de entidades en otro proyecto requiere trasladar los archivos generados y ajustar sus namespaces; los DTOs, servicios y reglas se escriben por separado.

La tabla de sustentación de los seis CreateMap está en [docs/Mapeos.md](docs/Mapeos.md). Las instrucciones de reconstrucción están en [docs/Comandos.md](docs/Comandos.md).

## Observaciones técnicas

1. La separación de responsabilidades permite cambiar las consultas del repositorio sin introducir SQL ni EF en los controladores. Las reglas se conservan en los servicios.
2. Los DTOs de entrada restringen los campos modificables; los de lectura publican una estructura estable sin navegación circular ni exposición de entidades completas.
3. La FK con eliminación restringida protege la relación en SQL. La comprobación del servicio produce un mensaje comprensible en el flujo normal; no equivale a una transacción que impida todas las carreras entre peticiones concurrentes.

## Conclusiones

1. AutoMapper concentra la conversión entre modelos y contratos. Validar el perfil al inicio detecta miembros de destino sin configuración antes de atender peticiones.
2. El mapeo explícito de NombreCliente permite enriquecer la respuesta sin devolver el objeto Cliente. Los mapeos de entrada ignoran Id y navegaciones para preservar el control del servidor.
3. Capturar ReglaNegocioException en los controladores mantiene las reglas independientes del protocolo HTTP. Este manejo descentralizado cumple el laboratorio; en una API mayor puede centralizarse con un filtro o manejador de excepciones para reducir repetición.

## Versiones y referencias

Se utilizan EF Core SQL Server, Design y Tools 10.0.12; AutoMapper 16.3.0 y Swashbuckle.AspNetCore 11.0.0. Se agregó Pkcs 10.0.13 para corregir las advertencias de vulnerabilidad de la dependencia transitiva anterior. El proyecto sigue dirigido a net10.0.

AutoMapper permite configurar su clave con `AutoMapper__LicenseKey` o secretos de usuario; no se incluye una clave en el repositorio. Consultar las condiciones aplicables y la [configuración oficial de licencia](https://docs.automapper.io/en/latest/License-configuration.html). El arranque sin clave utilizado en las pruebas emite la advertencia de licencia de la biblioteca.

Consultar también [ingeniería inversa de EF Core](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding) y el [aviso de Microsoft sobre Pkcs](https://github.com/advisories/GHSA-846f-f764-76mx).
"# lab13" 

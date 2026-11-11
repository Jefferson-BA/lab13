$ErrorActionPreference = 'Stop'
$raizLaboratorio = Split-Path -Parent $PSScriptRoot
Push-Location $raizLaboratorio
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo restaurar dotnet-ef.' }
    dotnet ef dbcontext scaffold 'Name=ConnectionStrings:VeterinariaDB' Microsoft.EntityFrameworkCore.SqlServer --project VeterinariaApp.Datos --startup-project VeterinariaApp.Api --context VeterinariaContext --context-dir ../artifacts/scaffold/Contexto --output-dir ../artifacts/scaffold/Entidades --namespace VeterinariaApp.Entidades --context-namespace VeterinariaApp.Datos --no-onconfiguring
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo generar el modelo. Comprobar la conexión y la carpeta de salida.' }
} finally {
    Pop-Location
}

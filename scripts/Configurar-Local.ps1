param(
    [string]$Servidor = '(localdb)\MSSQLLocalDB',
    [string]$BaseDatos = 'VeterinariaDB',
    [ValidateRange(1, 1440)][int]$MinutosExpiracion = 60
)
$ErrorActionPreference = 'Stop'
$proyectoApi = Join-Path (Split-Path -Parent $PSScriptRoot) 'VeterinariaApp.Api'
# La clave se genera en el equipo; no se imprime ni se escribe en el repositorio.
$generador = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$bytesClave = New-Object byte[] 48
$generador.GetBytes($bytesClave)
$generador.Dispose()
$claveJwt = [Convert]::ToBase64String($bytesClave)
$conexionLocal = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$conexionLocal['Data Source'] = $Servidor
$conexionLocal['Initial Catalog'] = $BaseDatos
$conexionLocal['Integrated Security'] = $true
$conexionLocal['TrustServerCertificate'] = $true
@{
    'Jwt:Clave' = $claveJwt
    'Jwt:MinutosExpiracion' = $MinutosExpiracion.ToString()
    'ConnectionStrings:VeterinariaDB' = $conexionLocal.ConnectionString
}.GetEnumerator() | ForEach-Object {
    & dotnet user-secrets set $_.Key $_.Value --project $proyectoApi | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'No se pudieron configurar los secretos locales.' }
}
Write-Host 'Conexión, clave JWT y expiración configuradas fuera del repositorio.'
Write-Host 'Los tokens emitidos con una clave anterior ya no serán válidos.'

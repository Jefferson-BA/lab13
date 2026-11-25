$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$api = Join-Path $raiz 'VeterinariaApp.Api'
[xml]$proyecto = Get-Content (Join-Path $api 'VeterinariaApp.Api.csproj') -Raw
$id = [string]$proyecto.Project.PropertyGroup.UserSecretsId
$archivo = Join-Path $env:APPDATA "Microsoft/UserSecrets/$id/secrets.json"
if (-not (Test-Path -LiteralPath $archivo)) {
    throw 'Configura primero los secretos de la API con scripts/Configurar-Local.ps1.'
}
$secretos = Get-Content -LiteralPath $archivo -Raw | ConvertFrom-Json
$config = Get-Content (Join-Path $api 'appsettings.json') -Raw | ConvertFrom-Json
$jwt = @{}
foreach ($nombre in @('Clave','Emisor','Audiencia')) {
    $valor = $secretos."Jwt:$nombre"
    if (-not $valor -and $secretos.Jwt) { $valor = $secretos.Jwt.$nombre }
    if (-not $valor) { $valor = $config.Jwt.$nombre }
    if (-not $valor) { throw "Falta Jwt:$nombre en la API." }
    $jwt["Jwt:$nombre"] = $valor
}
foreach ($nombre in @('GatewayApi','VeterinariaApp.Citas')) {
    $jwt | ConvertTo-Json | & dotnet user-secrets set --project (Join-Path $raiz $nombre) | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "No se pudieron configurar los secretos de $nombre." }
}
Write-Host 'JWT sincronizado con la API en los secretos locales de GatewayApi y Citas. No se imprimieron claves.'

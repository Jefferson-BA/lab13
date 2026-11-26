param(
    [Parameter(Mandatory)]
    [ValidateSet('GatewayApi', 'VeterinariaApp.Api', 'VeterinariaApp.Citas')]
    [string]$Proyecto,
    [string]$Archivo = '.env.local',
    [switch]$SoloValidar,
    [switch]$InicializarBaseDatos
)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
if (-not [IO.Path]::IsPathRooted($Archivo)) { $Archivo = Join-Path $raiz $Archivo }
if (-not (Test-Path -LiteralPath $Archivo -PathType Leaf)) {
    throw 'No existe el archivo de variables. Copia .env.example a .env.local y completa sus valores.'
}
$variables = @{}
$numero = 0
foreach ($linea in [IO.File]::ReadAllLines($Archivo)) {
    $numero++
    $texto = $linea.Trim()
    if (-not $texto -or $texto.StartsWith('#')) { continue }
    if ($texto -notmatch '^([A-Za-z_][A-Za-z0-9_]*)=(.*)$') {
        throw "Formato de variable invalido en la linea $numero."
    }
    $nombre = $Matches[1]
    $valor = $Matches[2].Trim()
    if ($variables.ContainsKey($nombre)) { throw "Variable repetida en la linea $numero." }
    if ($valor.StartsWith('"') -or $valor.StartsWith("'")) {
        $comilla = $valor.Substring(0,1)
        if ($valor.Length -lt 2 -or -not $valor.EndsWith($comilla)) {
            throw "Comillas sin cerrar en la linea $numero."
        }
        $valor = $valor.Substring(1, $valor.Length - 2)
    }
    $comun = $nombre -in @('ASPNETCORE_ENVIRONMENT','ASPNETCORE_HTTP_PORTS','Jwt__Clave','Jwt__Emisor','Jwt__Audiencia')
    $api = $Proyecto -eq 'VeterinariaApp.Api' -and $nombre -in @('ConnectionStrings__VeterinariaDB','Jwt__MinutosExpiracion','AutoMapper__LicenseKey','BaseDatos__Proveedor')
    $gateway = $Proyecto -eq 'GatewayApi' -and ($nombre -eq 'GlobalConfiguration__BaseUrl' -or $nombre.StartsWith('Routes__') -or $nombre -in @('Servicios__ApiUrl','Servicios__CitasUrl'))
    if ($comun -or $api -or $gateway) { $variables[$nombre] = $valor }
}
if ($SoloValidar) {
    Write-Host "Sintaxis correcta. $($variables.Count) variables aplicables a $Proyecto. No se validaron credenciales ni conectividad."
    exit 0
}
# Modifica solo el entorno de este proceso y sus hijos, sin imprimir valores.
foreach ($nombre in $variables.Keys) {
    [Environment]::SetEnvironmentVariable($nombre, $variables[$nombre], 'Process')
}
Push-Location $raiz
try {
    if ($InicializarBaseDatos) {
        if ($Proyecto -ne 'VeterinariaApp.Api') { throw 'La inicializacion solo se aplica a VeterinariaApp.Api.' }
        & dotnet run --project $Proyecto --no-launch-profile -- --inicializar-bd
    } elseif ($env:ASPNETCORE_ENVIRONMENT -eq 'Development') {
        & dotnet run --project $Proyecto --launch-profile http
    } else {
        & dotnet run --project $Proyecto --no-launch-profile
    }
    $resultado = $LASTEXITCODE
} finally {
    Pop-Location
}
exit $resultado

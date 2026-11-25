param(
    [Parameter(Mandatory)][Uri]$Gateway,
    [Parameter(Mandatory)][Uri]$Api,
    [Parameter(Mandatory)][Uri]$Citas
)
$ErrorActionPreference = 'Stop'
foreach ($url in @($Gateway, $Api, $Citas)) {
    if (-not $url.IsAbsoluteUri -or $url.Scheme -ne 'https' -or
        $url.AbsolutePath -ne '/' -or $url.Query -or $url.Fragment -or $url.UserInfo) {
        throw 'Usa URL HTTPS de origen, sin rutas, credenciales ni consultas.'
    }
}
$raiz = Split-Path -Parent $PSScriptRoot
$archivo = Join-Path $raiz 'GatewayApi/ocelot.json'
$config = Get-Content -LiteralPath $archivo -Raw | ConvertFrom-Json
foreach ($ruta in $config.Routes) {
    $destino = if ($ruta.UpstreamPathTemplate.StartsWith('/gateway/citas/')) { $Citas } else { $Api }
    $ruta.DownstreamScheme = 'https'
    $ruta.DownstreamHostAndPorts[0].Host = $destino.DnsSafeHost
    $ruta.DownstreamHostAndPorts[0].Port = $destino.Port
}
$config.GlobalConfiguration.BaseUrl = $Gateway.GetLeftPart([UriPartial]::Authority)
$salida = Join-Path $raiz 'GatewayApi/ocelot.Production.json'
[IO.File]::WriteAllText($salida, ($config | ConvertTo-Json -Depth 20), [Text.UTF8Encoding]::new($false))
Write-Host 'Configuracion de produccion generada. Reconstruye y publica el gateway para aplicarla.'

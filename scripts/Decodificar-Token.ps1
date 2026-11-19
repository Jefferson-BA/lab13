param([string]$Token)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($Token)) { $Token = Read-Host 'Pegar un token de prueba' }
$partes = $Token.Split('.')
if ($partes.Length -ne 3) { throw 'Se esperaba un JWT con tres segmentos.' }
function Convertir-Base64Url([string]$Valor) {
    $base64 = $Valor.Replace('-', '+').Replace('_', '/')
    $base64 = $base64.PadRight($base64.Length + (4 - $base64.Length % 4) % 4, '=')
    [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($base64)) | ConvertFrom-Json
}
Write-Host 'Encabezado del token'
Convertir-Base64Url $partes[0] | ConvertTo-Json
Write-Host 'Claims del token'
Convertir-Base64Url $partes[1] | ConvertTo-Json
Write-Host 'Decodificar permite leer; la API comprueba la firma al recibir el token.'

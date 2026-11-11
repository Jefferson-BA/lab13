param([string]$Servidor = '(localdb)\MSSQLLocalDB')
$ErrorActionPreference = 'Stop'
$raizLaboratorio = Split-Path -Parent $PSScriptRoot
if ($Servidor -eq '(localdb)\MSSQLLocalDB') {
    & SqlLocalDB start MSSQLLocalDB
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo iniciar LocalDB.' }
}
& sqlcmd -S $Servidor -E -b -f 65001 -i (Join-Path $raizLaboratorio 'VeterinariaDB.sql')
if ($LASTEXITCODE -ne 0) { throw 'No se pudo inicializar VeterinariaDB.' }
Write-Host 'VeterinariaDB lista con los datos de ejemplo.'

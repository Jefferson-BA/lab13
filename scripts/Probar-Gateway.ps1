param(
    [string]$BaseUrl = 'http://localhost:5143',
    [switch]$CitasDetenidas,
    [string]$CitasDirecto = 'http://localhost:5020',
    [string]$Salida = ''
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$cliente = [System.Net.Http.HttpClient]::new()
$cliente.Timeout = [TimeSpan]::FromSeconds(30)
$resultados = [System.Collections.Generic.List[object]]::new()
function Probar([string]$Metodo, [string]$Ruta, [int]$Esperado, $Datos = $null, [string]$Token = '', [string]$Origen = $BaseUrl) {
    $peticion = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Metodo), "$Origen$Ruta")
    if ($null -ne $Datos) {
        $peticion.Content = [System.Net.Http.StringContent]::new(($Datos | ConvertTo-Json), [Text.Encoding]::UTF8, 'application/json')
    }
    if ($Token) { $peticion.Headers.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $Token) }
    $respuesta = $cliente.SendAsync($peticion).GetAwaiter().GetResult()
    $contenido = $respuesta.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $estado = [int]$respuesta.StatusCode
    $resultados.Add([pscustomobject]@{
        fechaUTC = [DateTime]::UtcNow.ToString('o')
        metodo = $Metodo
        url = "$Origen$Ruta"
        esperado = $Esperado
        recibido = $estado
        correcto = ($estado -eq $Esperado)
        conToken = [bool]$Token
    })
    $respuesta.Dispose()
    $peticion.Dispose()
    if ($estado -ne $Esperado) { throw "$Metodo $Ruta esperaba $Esperado y recibio $estado." }
    Write-Host "OK $estado $Metodo $Origen$Ruta"
    if ($contenido) { return ($contenido | ConvertFrom-Json) }
}
# Se crea una cuenta Recepcionista de prueba. No se imprimen claves ni tokens.
$sufijo = [Guid]::NewGuid().ToString('N')
$cuenta = @{ correo = "s15-$sufijo@example.com"; clave = [Guid]::NewGuid().ToString('N') + '!a9' }
try {
    $null = Probar GET '/gateway/clientes' 200
    $null = Probar GET '/gateway/mascotas' 200
    $null = Probar GET '/gateway/citas' 401
    $null = Probar GET '/gateway/auth/perfil' 401
    $null = Probar POST '/gateway/clientes' 401 @{}
    $null = Probar GET '/gateway/citas' 401 $null 'token-invalido'
    $null = Probar POST '/gateway/auth/registro' 201 $cuenta
    $null = Probar POST '/gateway/auth/registro' 409 $cuenta
    $login = Probar POST '/gateway/auth/login' 200 $cuenta
    $token = $login.token
    $null = Probar GET '/gateway/auth/perfil' 200 $null $token
    if ($CitasDetenidas) {
        $null = Probar GET '/gateway/citas' 502 $null $token
        $null = Probar GET '/gateway/clientes' 200
    } else {
        $null = Probar GET '/gateway/citas' 200 $null $token
        $null = Probar GET '/gateway/citas/1' 200 $null $token
        $null = Probar GET '/gateway/citas/999999' 404 $null $token
        $null = Probar GET '/api/citas' 401 $null '' $CitasDirecto
        $null = Probar GET '/api/citas/1' 401 $null '' $CitasDirecto
        $null = Probar GET '/api/citas' 401 $null 'token-invalido' $CitasDirecto
        $null = Probar GET '/api/citas' 200 $null $token $CitasDirecto
    }
    # Recepcionista no tiene permiso para escribir mascotas (se exige Veterinario).
    $null = Probar POST '/gateway/mascotas' 403 @{} $token
    $datos = @{
        nombre = 'Prueba S15'; apellido = 'Gateway'
        dni = (Get-Random -Minimum 10000000 -Maximum 99999999).ToString()
        telefono = '999123456'; email = "cliente-$sufijo@example.com"
    }
    $creado = Probar POST '/gateway/clientes' 201 $datos $token
    try {
        $null = Probar GET "/gateway/clientes/$($creado.id)" 200
        $datos.nombre = 'Prueba actualizada'
        $null = Probar PUT "/gateway/clientes/$($creado.id)" 204 $datos $token
    } finally {
        $null = Probar DELETE "/gateway/clientes/$($creado.id)" 204 $null $token
    }
    $null = Probar GET "/gateway/clientes/$($creado.id)" 404
    $null = Probar GET '/gateway/ruta-inexistente' 404
    Write-Host 'Pruebas correctas. Cliente temporal eliminado; queda la cuenta de prueba Recepcionista.'
} finally {
    $cliente.Dispose()
    if ($Salida) {
        $destino = [IO.Path]::GetFullPath($Salida)
        $null = [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destino))
        [IO.File]::WriteAllText($destino, (ConvertTo-Json -InputObject @($resultados.ToArray()) -Depth 5),
            [Text.UTF8Encoding]::new($false))
        Write-Host 'Registro de estados HTTP guardado sin claves, tokens ni cuerpos privados.'
    }
}

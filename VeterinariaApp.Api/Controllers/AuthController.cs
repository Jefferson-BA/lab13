using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinariaApp.Entidades.DTOs;
using VeterinariaApp.Negocio;
using VeterinariaApp.Negocio.Servicios;

namespace VeterinariaApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("registro")]
    [AllowAnonymous]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioDto>> Registro(RegistroDto dto, CancellationToken ct)
    {
        try
        {
            return StatusCode(StatusCodes.Status201Created, await auth.RegistrarAsync(dto, ct));
        }
        catch (ReglaNegocioException ex)
        {
            return Problem(statusCode: 409, title: "Regla de negocio", detail: ex.Message);
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<TokenDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenDto>> Login(LoginDto dto, CancellationToken ct)
    {
        var token = await auth.LoginAsync(dto, ct);
        return token is null
            ? Unauthorized(new { mensaje = "Correo o clave incorrectos." })
            : Ok(token);
    }

    [HttpGet("perfil")]
    [ProducesResponseType<PerfilDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<PerfilDto> Perfil() => Ok(new PerfilDto
    {
        Correo = User.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value,
        Rol = User.Claims.First(c => c.Type == "rol").Value
    });
}

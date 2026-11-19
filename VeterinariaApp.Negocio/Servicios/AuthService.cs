using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VeterinariaApp.Datos;
using VeterinariaApp.Datos.Repositorios;
using VeterinariaApp.Entidades;
using VeterinariaApp.Entidades.DTOs;
using VeterinariaApp.Negocio.Configuracion;

namespace VeterinariaApp.Negocio.Servicios;

public class AuthService(IUsuarioRepository usuarios, IMapper mapper, IOptions<JwtOpciones> opciones) : IAuthService
{
    // Hace una comprobación BCrypt incluso si el correo no existe.
    private static readonly string HashSustituto = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), 12);

    public async Task<UsuarioDto> RegistrarAsync(RegistroDto dto, CancellationToken ct = default)
    {
        var correo = dto.Correo.Trim().ToLowerInvariant();
        if (await usuarios.BuscarPorCorreoAsync(correo, ct) is not null)
            throw new ReglaNegocioException("El correo ya se encuentra registrado.");

        var usuario = new Usuario
        {
            Correo = correo,
            ClaveHash = BCrypt.Net.BCrypt.HashPassword(dto.Clave, 12),
            Rol = Roles.Recepcionista
        };
        await usuarios.AgregarAsync(usuario, ct);
        try
        {
            await usuarios.GuardarAsync(ct);
        }
        catch (CorreoDuplicadoException)
        {
            // El índice único también cubre registros concurrentes del mismo correo.
            throw new ReglaNegocioException("El correo ya se encuentra registrado.");
        }
        return mapper.Map<UsuarioDto>(usuario);
    }

    public async Task<TokenDto?> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var usuario = await usuarios.BuscarPorCorreoAsync(dto.Correo.Trim().ToLowerInvariant(), ct);
        var claveValida = BCrypt.Net.BCrypt.Verify(dto.Clave, usuario?.ClaveHash ?? HashSustituto);
        if (usuario is null || !claveValida) return null;

        var jwt = opciones.Value;
        var ahora = DateTime.UtcNow;
        var expiracion = ahora.AddMinutes(jwt.MinutosExpiracion);
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Email, usuario.Correo),
            new("rol", usuario.Rol),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];
        var token = new JwtSecurityToken(jwt.Emisor, jwt.Audiencia, claims, ahora, expiracion,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)),
                SecurityAlgorithms.HmacSha256));
        return new TokenDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            FechaExpiracion = expiracion
        };
    }
}

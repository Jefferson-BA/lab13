using VeterinariaApp.Entidades.DTOs;

namespace VeterinariaApp.Negocio.Servicios;

public interface IAuthService
{
    Task<UsuarioDto> RegistrarAsync(RegistroDto dto, CancellationToken ct = default);
    Task<TokenDto?> LoginAsync(LoginDto dto, CancellationToken ct = default);
}

using VeterinariaApp.Entidades;

namespace VeterinariaApp.Datos.Repositorios;

public interface IUsuarioRepository
{
    Task<Usuario?> BuscarPorCorreoAsync(string correo, CancellationToken ct = default);
    Task AgregarAsync(Usuario usuario, CancellationToken ct = default);
    Task GuardarAsync(CancellationToken ct = default);
}

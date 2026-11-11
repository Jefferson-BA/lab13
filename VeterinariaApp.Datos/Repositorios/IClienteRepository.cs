using VeterinariaApp.Entidades;
namespace VeterinariaApp.Datos.Repositorios;

public interface IClienteRepository
{
    Task<List<Cliente>> ObtenerTodosAsync(CancellationToken ct = default);
    Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExisteAsync(int id, CancellationToken ct = default);
    Task AgregarAsync(Cliente entidad, CancellationToken ct = default);
    Task ActualizarAsync(Cliente entidad, CancellationToken ct = default);
    Task EliminarAsync(Cliente entidad, CancellationToken ct = default);
    Task<bool> TieneMascotasAsync(int id, CancellationToken ct = default);
}

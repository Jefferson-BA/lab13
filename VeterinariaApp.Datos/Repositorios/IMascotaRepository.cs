using VeterinariaApp.Entidades;
namespace VeterinariaApp.Datos.Repositorios;

public interface IMascotaRepository
{
    Task<List<Mascota>> ObtenerTodosAsync(CancellationToken ct = default);
    Task<Mascota?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExisteAsync(int id, CancellationToken ct = default);
    Task AgregarAsync(Mascota entidad, CancellationToken ct = default);
    Task ActualizarAsync(Mascota entidad, CancellationToken ct = default);
    Task EliminarAsync(Mascota entidad, CancellationToken ct = default);
    Task<List<Mascota>> PorClienteAsync(int clienteId, CancellationToken ct = default);
    Task<List<Mascota>> BuscarAsync(string especie, CancellationToken ct = default);
}

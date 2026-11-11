using VeterinariaApp.Entidades.DTOs;
namespace VeterinariaApp.Negocio.Servicios;

public interface IMascotaService
{
    Task<List<MascotaDto>> ObtenerTodosAsync(CancellationToken ct = default);
    Task<MascotaDto?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<MascotaDto> CrearAsync(MascotaCrearDto dto, CancellationToken ct = default);
    Task<bool> ActualizarAsync(int id, MascotaActualizarDto dto, CancellationToken ct = default);
    Task<bool> EliminarAsync(int id, CancellationToken ct = default);
    Task<List<MascotaDto>> BuscarAsync(string especie, CancellationToken ct = default);
}

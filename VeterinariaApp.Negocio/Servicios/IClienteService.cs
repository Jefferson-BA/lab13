using VeterinariaApp.Entidades.DTOs;
namespace VeterinariaApp.Negocio.Servicios;

public interface IClienteService
{
    Task<List<ClienteDto>> ObtenerTodosAsync(CancellationToken ct = default);
    Task<ClienteDto?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<ClienteDto> CrearAsync(ClienteCrearDto dto, CancellationToken ct = default);
    Task<bool> ActualizarAsync(int id, ClienteActualizarDto dto, CancellationToken ct = default);
    Task<bool> EliminarAsync(int id, CancellationToken ct = default);
    Task<List<MascotaDto>?> ObtenerMascotasAsync(int clienteId, CancellationToken ct = default);
}

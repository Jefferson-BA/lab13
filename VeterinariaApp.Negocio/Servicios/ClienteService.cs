using AutoMapper;
using VeterinariaApp.Datos.Repositorios;
using VeterinariaApp.Entidades;
using VeterinariaApp.Entidades.DTOs;
namespace VeterinariaApp.Negocio.Servicios;

public class ClienteService(IClienteRepository clientes, IMascotaRepository mascotas, IMapper mapper) : IClienteService
{
    public async Task<List<ClienteDto>> ObtenerTodosAsync(CancellationToken ct = default) =>
        mapper.Map<List<ClienteDto>>(await clientes.ObtenerTodosAsync(ct));

    public async Task<ClienteDto?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        var entidad = await clientes.ObtenerPorIdAsync(id, ct);
        return entidad is null ? null : mapper.Map<ClienteDto>(entidad);
    }

    public async Task<ClienteDto> CrearAsync(ClienteCrearDto dto, CancellationToken ct = default)
    {
        var entidad = mapper.Map<Cliente>(dto);
        await clientes.AgregarAsync(entidad, ct);
        return mapper.Map<ClienteDto>(entidad);
    }

    public async Task<bool> ActualizarAsync(int id, ClienteActualizarDto dto, CancellationToken ct = default)
    {
        var entidad = await clientes.ObtenerPorIdAsync(id, ct);
        if (entidad is null) return false;
        mapper.Map(dto, entidad);
        await clientes.ActualizarAsync(entidad, ct);
        return true;
    }

    public async Task<bool> EliminarAsync(int id, CancellationToken ct = default)
    {
        var entidad = await clientes.ObtenerPorIdAsync(id, ct);
        if (entidad is null) return false;
        if (await clientes.TieneMascotasAsync(id, ct))
            throw new ReglaNegocioException("No se puede eliminar un cliente con mascotas registradas.");
        await clientes.EliminarAsync(entidad, ct);
        return true;
    }

    public async Task<List<MascotaDto>?> ObtenerMascotasAsync(int clienteId, CancellationToken ct = default)
    {
        if (!await clientes.ExisteAsync(clienteId, ct)) return null;
        return mapper.Map<List<MascotaDto>>(await mascotas.PorClienteAsync(clienteId, ct));
    }
}

using AutoMapper;
using VeterinariaApp.Datos.Repositorios;
using VeterinariaApp.Entidades;
using VeterinariaApp.Entidades.DTOs;
namespace VeterinariaApp.Negocio.Servicios;

public class MascotaService(IClienteRepository clientes, IMascotaRepository mascotas, IMapper mapper) : IMascotaService
{
    public async Task<List<MascotaDto>> ObtenerTodosAsync(CancellationToken ct = default) =>
        mapper.Map<List<MascotaDto>>(await mascotas.ObtenerTodosAsync(ct));

    public async Task<MascotaDto?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        var entidad = await mascotas.ObtenerPorIdAsync(id, ct);
        return entidad is null ? null : mapper.Map<MascotaDto>(entidad);
    }

    public async Task<MascotaDto> CrearAsync(MascotaCrearDto dto, CancellationToken ct = default)
    {
        await ValidarAsync(dto.Edad!.Value, dto.ClienteId!.Value, ct);
        var entidad = mapper.Map<Mascota>(dto);
        await mascotas.AgregarAsync(entidad, ct);
        entidad = (await mascotas.ObtenerPorIdAsync(entidad.Id, ct))!;
        return mapper.Map<MascotaDto>(entidad);
    }

    public async Task<bool> ActualizarAsync(int id, MascotaActualizarDto dto, CancellationToken ct = default)
    {
        var entidad = await mascotas.ObtenerPorIdAsync(id, ct);
        if (entidad is null) return false;
        await ValidarAsync(dto.Edad!.Value, dto.ClienteId!.Value, ct);
        mapper.Map(dto, entidad);
        await mascotas.ActualizarAsync(entidad, ct);
        return true;
    }

    public async Task<bool> EliminarAsync(int id, CancellationToken ct = default)
    {
        var entidad = await mascotas.ObtenerPorIdAsync(id, ct);
        if (entidad is null) return false;
        await mascotas.EliminarAsync(entidad, ct);
        return true;
    }

    public async Task<List<MascotaDto>> BuscarAsync(string especie, CancellationToken ct = default) =>
        mapper.Map<List<MascotaDto>>(await mascotas.BuscarAsync(especie, ct));

    private async Task ValidarAsync(int edad, int clienteId, CancellationToken ct)
    {
        if (edad < 0 || edad > 30)
            throw new ReglaNegocioException("La edad de la mascota debe estar entre 0 y 30 años.");
        if (!await clientes.ExisteAsync(clienteId, ct))
            throw new ReglaNegocioException("El cliente indicado no existe.");
    }
}

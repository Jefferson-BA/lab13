using Microsoft.EntityFrameworkCore;
using VeterinariaApp.Entidades;
namespace VeterinariaApp.Datos.Repositorios;

public class MascotaRepository(VeterinariaContext context) : IMascotaRepository
{
    public async Task<List<Mascota>> ObtenerTodosAsync(CancellationToken ct = default) =>
        await context.Mascotas.Include(x => x.Cliente).AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct);

    public async Task<Mascota?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        await context.Mascotas.Include(x => x.Cliente).FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<bool> ExisteAsync(int id, CancellationToken ct = default) =>
        await context.Mascotas.AnyAsync(x => x.Id == id, ct);

    public async Task AgregarAsync(Mascota entidad, CancellationToken ct = default)
    {
        await context.Mascotas.AddAsync(entidad, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task ActualizarAsync(Mascota entidad, CancellationToken ct = default)
    {
        context.Mascotas.Update(entidad);
        await context.SaveChangesAsync(ct);
    }

    public async Task EliminarAsync(Mascota entidad, CancellationToken ct = default)
    {
        context.Mascotas.Remove(entidad);
        await context.SaveChangesAsync(ct);
    }

    public async Task<List<Mascota>> PorClienteAsync(int clienteId, CancellationToken ct = default) =>
        await context.Mascotas.Include(x => x.Cliente).AsNoTracking()
            .Where(x => x.ClienteId == clienteId).OrderBy(x => x.Id).ToListAsync(ct);

    public async Task<List<Mascota>> BuscarAsync(string especie, CancellationToken ct = default) =>
        await context.Mascotas.Include(x => x.Cliente).AsNoTracking()
            .Where(x => x.Especie == especie.Trim()).OrderBy(x => x.Id).ToListAsync(ct);
}

using Microsoft.EntityFrameworkCore;
using VeterinariaApp.Entidades;
namespace VeterinariaApp.Datos.Repositorios;

public class ClienteRepository(VeterinariaContext context) : IClienteRepository
{
    public async Task<List<Cliente>> ObtenerTodosAsync(CancellationToken ct = default) =>
        await context.Clientes.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct);

    public async Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        await context.Clientes.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<bool> ExisteAsync(int id, CancellationToken ct = default) =>
        await context.Clientes.AnyAsync(x => x.Id == id, ct);

    public async Task AgregarAsync(Cliente entidad, CancellationToken ct = default)
    {
        await context.Clientes.AddAsync(entidad, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task ActualizarAsync(Cliente entidad, CancellationToken ct = default)
    {
        context.Clientes.Update(entidad);
        await context.SaveChangesAsync(ct);
    }

    public async Task EliminarAsync(Cliente entidad, CancellationToken ct = default)
    {
        context.Clientes.Remove(entidad);
        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> TieneMascotasAsync(int id, CancellationToken ct = default) =>
        await context.Mascotas.AnyAsync(x => x.ClienteId == id, ct);
}

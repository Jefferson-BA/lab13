using Microsoft.Data.SqlClient;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using VeterinariaApp.Entidades;

namespace VeterinariaApp.Datos.Repositorios;

public class UsuarioRepository(VeterinariaContext context) : IUsuarioRepository
{
    public async Task<Usuario?> BuscarPorCorreoAsync(string correo, CancellationToken ct = default) =>
        await context.Usuarios.AsNoTracking().SingleOrDefaultAsync(x => x.Correo == correo, ct);

    public async Task AgregarAsync(Usuario usuario, CancellationToken ct = default) =>
        await context.Usuarios.AddAsync(usuario, ct);

    public async Task GuardarAsync(CancellationToken ct = default)
    {
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException { Number: 2601 or 2627 } ||
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "UX_Usuarios_Correo" })
        {
            throw new CorreoDuplicadoException(ex);
        }
    }
}

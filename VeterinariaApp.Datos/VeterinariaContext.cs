using Microsoft.EntityFrameworkCore;
using VeterinariaApp.Entidades;

namespace VeterinariaApp.Datos;

public class VeterinariaContext(DbContextOptions<VeterinariaContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Mascota> Mascotas => Set<Mascota>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios", t => t.HasCheckConstraint("CK_Usuarios_Rol",
                "[Rol] IN ('Recepcionista', 'Veterinario')"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Correo).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.Correo).IsUnique().HasDatabaseName("UX_Usuarios_Correo");
            e.Property(x => x.ClaveHash).HasMaxLength(100).IsRequired();
            e.Property(x => x.Rol).HasMaxLength(30).IsRequired();
        });
        modelBuilder.Entity<Cliente>(e =>
        {
            e.ToTable("Clientes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
            e.Property(x => x.Apellido).HasMaxLength(80).IsRequired();
            e.Property(x => x.Dni).HasMaxLength(8).IsRequired();
            e.Property(x => x.Telefono).HasMaxLength(20).IsRequired();
            e.Property(x => x.Email).HasMaxLength(120).IsRequired();
        });
        modelBuilder.Entity<Mascota>(e =>
        {
            e.ToTable("Mascotas", t => t.HasCheckConstraint("CK_Mascotas_Edad", "[Edad] >= 0 AND [Edad] <= 30"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
            e.Property(x => x.Especie).HasMaxLength(50).IsRequired();
            e.Property(x => x.Raza).HasMaxLength(80).IsRequired();
            e.HasOne(x => x.Cliente).WithMany(x => x.Mascotas)
                .HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

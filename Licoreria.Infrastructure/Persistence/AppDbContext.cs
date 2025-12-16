using Licoreria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Licoreria.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Jornada> Jornadas => Set<Jornada>();
    public DbSet<Mesa> Mesas => Set<Mesa>();
    public DbSet<OrdenMesa> OrdenesMesa => Set<OrdenMesa>();
    public DbSet<DetalleOrden> DetallesOrden => Set<DetalleOrden>();
    public DbSet<Pago> Pagos => Set<Pago>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Usuario>().HasKey(x => x.IdUsuario);
        mb.Entity<Categoria>().HasKey(x => x.IdCategoria);
        mb.Entity<Producto>().HasKey(x => x.IdProducto);
        mb.Entity<Jornada>().HasKey(x => x.IdJornada);
        mb.Entity<Mesa>().HasKey(x => x.IdMesa);
        mb.Entity<OrdenMesa>().HasKey(x => x.IdOrden);
        mb.Entity<DetalleOrden>().HasKey(x => x.IdDetalle);
        mb.Entity<Pago>().HasKey(x => x.IdPago);

        mb.Entity<Producto>()
            .HasOne(p => p.Categoria)
            .WithMany(c => c.Productos)
            .HasForeignKey(p => p.IdCategoria);

        mb.Entity<OrdenMesa>()
            .HasOne(o => o.Mesa)
            .WithMany()
            .HasForeignKey(o => o.IdMesa);

        mb.Entity<OrdenMesa>()
            .HasOne(o => o.Jornada)
            .WithMany()
            .HasForeignKey(o => o.IdJornada);

        mb.Entity<DetalleOrden>()
            .HasOne(d => d.Orden)
            .WithMany(o => o.Detalles)
            .HasForeignKey(d => d.IdOrden);

        mb.Entity<DetalleOrden>()
            .HasOne(d => d.Producto)
            .WithMany()
            .HasForeignKey(d => d.IdProducto);

        mb.Entity<Pago>()
            .HasOne(p => p.Orden)
            .WithMany(o => o.Pagos)
            .HasForeignKey(p => p.IdOrden);

        mb.Entity<Mesa>().Property(x => x.Estado).HasMaxLength(20);
        mb.Entity<Jornada>().Property(x => x.Estado).HasMaxLength(20);
        mb.Entity<Usuario>().Property(x => x.Rol).HasMaxLength(20);
        mb.Entity<OrdenMesa>().Property(x => x.Estado).HasMaxLength(20);
        mb.Entity<OrdenMesa>().Property(x => x.TipoOrden).HasMaxLength(20);

        base.OnModelCreating(mb);
    }
}

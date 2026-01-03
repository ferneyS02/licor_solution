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
        // =========================
        // KEYS
        // =========================
        mb.Entity<Usuario>().HasKey(x => x.IdUsuario);
        mb.Entity<Categoria>().HasKey(x => x.IdCategoria);
        mb.Entity<Producto>().HasKey(x => x.IdProducto);
        mb.Entity<Jornada>().HasKey(x => x.IdJornada);
        mb.Entity<Mesa>().HasKey(x => x.IdMesa);
        mb.Entity<OrdenMesa>().HasKey(x => x.IdOrden);
        mb.Entity<DetalleOrden>().HasKey(x => x.IdDetalle);
        mb.Entity<Pago>().HasKey(x => x.IdPago);

        // =========================
        // PROPERTIES (Lengths/Precision)
        // =========================
        mb.Entity<Usuario>().Property(x => x.Nombre).HasMaxLength(60).IsRequired();
        mb.Entity<Usuario>().Property(x => x.Rol).HasMaxLength(20).IsRequired(); // Admin | Vendedor | Sistema
        mb.Entity<Usuario>().Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
        mb.Entity<Usuario>().HasIndex(x => x.Nombre); // para login (no unique para no romper datos existentes)

        mb.Entity<Categoria>().Property(x => x.Nombre).HasMaxLength(80).IsRequired();

        mb.Entity<Producto>().Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        mb.Entity<Producto>().Property(x => x.Imagen).HasMaxLength(200);
        mb.Entity<Producto>().Property(x => x.PrecioActual).HasPrecision(18, 2);

        mb.Entity<Mesa>().Property(x => x.Nombre).HasMaxLength(20).IsRequired();
        mb.Entity<Mesa>().Property(x => x.Estado).HasMaxLength(20);

        mb.Entity<Jornada>().Property(x => x.Estado).HasMaxLength(20);

        mb.Entity<OrdenMesa>().Property(x => x.Estado).HasMaxLength(20);
        mb.Entity<OrdenMesa>().Property(x => x.TipoPago).HasMaxLength(20);

        mb.Entity<DetalleOrden>().Property(x => x.NombreProducto).HasMaxLength(160).IsRequired();
        mb.Entity<DetalleOrden>().Property(x => x.PrecioUnitario).HasPrecision(18, 2);
        mb.Entity<DetalleOrden>().Property(x => x.Total).HasPrecision(18, 2);

        mb.Entity<Pago>().Property(x => x.TipoPago).HasMaxLength(20).IsRequired();
        mb.Entity<Pago>().Property(x => x.MontoBase).HasPrecision(18, 2);
        mb.Entity<Pago>().Property(x => x.Recargo).HasPrecision(18, 2);
        mb.Entity<Pago>().Property(x => x.MontoFinal).HasPrecision(18, 2);

        // =========================
        // RELATIONSHIPS
        // =========================

        // Producto -> Categoria (RESTRICT para no borrar categoría y llevarse productos)
        mb.Entity<Producto>()
            .HasOne(p => p.Categoria)
            .WithMany(c => c.Productos)
            .HasForeignKey(p => p.IdCategoria)
            .OnDelete(DeleteBehavior.Restrict);

        // ✅ ESTA ERA LA CAUSA DE “NO ABRE MESAS”
        // Evita columnas sombra (MesaIdMesa/JornadaIdJornada)
        mb.Entity<OrdenMesa>()
            .HasOne(o => o.Mesa)
            .WithMany()
            .HasForeignKey(o => o.IdMesa)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<OrdenMesa>()
            .HasOne(o => o.Jornada)
            .WithMany()
            .HasForeignKey(o => o.IdJornada)
            .OnDelete(DeleteBehavior.Restrict);

        // ✅ IMPORTANTE: también declarar Usuario para evitar columnas sombra y controlar borrados
        mb.Entity<OrdenMesa>()
            .HasOne(o => o.Usuario)
            .WithMany()
            .HasForeignKey(o => o.IdUsuario)
            .OnDelete(DeleteBehavior.Restrict);

        // ✅ CLAVE PARA ELIMINAR PRODUCTOS “BIEN”:
        // Si un producto ya está en ventas (DetalleOrden), NO se puede borrar (protege histórico).
        mb.Entity<DetalleOrden>()
            .HasOne(d => d.Producto)
            .WithMany()
            .HasForeignKey(d => d.IdProducto)
            .OnDelete(DeleteBehavior.Restrict);

        // Orden -> Detalles (CASCADE: si eliminas una orden, elimina sus detalles)
        mb.Entity<DetalleOrden>()
            .HasOne(d => d.Orden)
            .WithMany(o => o.Detalles)
            .HasForeignKey(d => d.IdOrden)
            .OnDelete(DeleteBehavior.Cascade);

        // Orden -> Pagos (CASCADE)
        mb.Entity<Pago>()
            .HasOne(p => p.Orden)
            .WithMany(o => o.Pagos)
            .HasForeignKey(p => p.IdOrden)
            .OnDelete(DeleteBehavior.Cascade);

        base.OnModelCreating(mb);
    }
}

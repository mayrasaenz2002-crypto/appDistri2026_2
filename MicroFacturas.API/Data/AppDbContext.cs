using Microsoft.EntityFrameworkCore;
using MicroFacturas.API.Models;

namespace MicroFacturas.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Factura> Facturas { get; set; }

    public DbSet<DetalleFactura> DetallesFactura { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Factura>()
            .HasMany(f => f.Detalles)
            .WithOne(d => d.Factura)
            .HasForeignKey(d => d.FacturaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Factura>()
            .Property(f => f.Subtotal)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Factura>()
            .Property(f => f.Iva)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Factura>()
            .Property(f => f.Total)
            .HasPrecision(18, 2);

        modelBuilder.Entity<DetalleFactura>()
            .Property(d => d.PrecioUnitario)
            .HasPrecision(18, 2);

        modelBuilder.Entity<DetalleFactura>()
            .Property(d => d.Subtotal)
            .HasPrecision(18, 2);
    }
}
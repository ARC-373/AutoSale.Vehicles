using AutoSale.Domain.Catalog;
using AutoSale.Domain.Reservations;
using AutoSale.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace AutoSale.Infrastructure.Persistence;

public sealed class AutoSaleDbContext : DbContext
{
    public AutoSaleDbContext(DbContextOptions<AutoSaleDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<VehicleReservation> VehicleReservations => Set<VehicleReservation>();

    public DbSet<CatalogOutbox> CatalogOutbox => Set<CatalogOutbox>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AutoSaleDbContext).Assembly);
    }
}

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AutoSaleDbContext).Assembly);
    }
}

using AutoSale.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoSale.Infrastructure.Persistence.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles", table =>
        {
            table.HasCheckConstraint("ck_vehicles_price_positive", "price > 0");
            table.HasCheckConstraint("ck_vehicles_year", "year >= 1886");
            table.HasCheckConstraint("ck_vehicles_version", "version > 0");
            table.HasCheckConstraint(
                "ck_vehicles_status_references",
                "(status = 'Available' AND reservation_sale_id IS NULL AND sold_sale_id IS NULL) OR " +
                "(status = 'Reserved' AND reservation_sale_id IS NOT NULL AND sold_sale_id IS NULL) OR " +
                "(status = 'Sold' AND reservation_sale_id IS NULL AND sold_sale_id IS NOT NULL)");
        });

        builder.HasKey(vehicle => vehicle.Id);

        builder.Property(vehicle => vehicle.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(vehicle => vehicle.Make)
            .HasColumnName("make")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(vehicle => vehicle.Model)
            .HasColumnName("model")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(vehicle => vehicle.Year)
            .HasColumnName("year")
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(vehicle => vehicle.Color)
            .HasColumnName("color")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(vehicle => vehicle.Price)
            .HasColumnName("price")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(vehicle => vehicle.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(vehicle => vehicle.ReservationSaleId)
            .HasColumnName("reservation_sale_id");

        builder.Property(vehicle => vehicle.SoldSaleId)
            .HasColumnName("sold_sale_id");

        builder.Property(vehicle => vehicle.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(vehicle => vehicle.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(vehicle => vehicle.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasIndex(vehicle => new { vehicle.Status, vehicle.Price, vehicle.Id })
            .HasDatabaseName("ix_vehicles_status_price_id");
    }
}

using AutoSale.Domain.Reservations;
using AutoSale.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoSale.Infrastructure.Persistence.Configurations;

public sealed class VehicleReservationConfiguration : IEntityTypeConfiguration<VehicleReservation>
{
    public void Configure(EntityTypeBuilder<VehicleReservation> builder)
    {
        builder.ToTable("vehicle_reservations", table =>
        {
            table.HasCheckConstraint("ck_vehicle_reservations_price_positive", "price_snapshot > 0");
            table.HasCheckConstraint("ck_vehicle_reservations_version", "vehicle_version_snapshot > 0");
            table.HasCheckConstraint(
                "ck_vehicle_reservations_terminal_timestamp",
                "(status = 'Reserved' AND confirmed_at_utc IS NULL AND released_at_utc IS NULL) OR " +
                "(status = 'Confirmed' AND confirmed_at_utc IS NOT NULL AND released_at_utc IS NULL) OR " +
                "(status = 'Released' AND confirmed_at_utc IS NULL AND released_at_utc IS NOT NULL)");
        });

        builder.HasKey(reservation => reservation.SaleId);

        builder.Property(reservation => reservation.SaleId)
            .HasColumnName("sale_id")
            .ValueGeneratedNever();

        builder.Property(reservation => reservation.VehicleId)
            .HasColumnName("vehicle_id")
            .IsRequired();

        builder.Property(reservation => reservation.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(reservation => reservation.MakeSnapshot)
            .HasColumnName("make_snapshot")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(reservation => reservation.ModelSnapshot)
            .HasColumnName("model_snapshot")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(reservation => reservation.YearSnapshot)
            .HasColumnName("year_snapshot")
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(reservation => reservation.ColorSnapshot)
            .HasColumnName("color_snapshot")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(reservation => reservation.PriceSnapshot)
            .HasColumnName("price_snapshot")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(reservation => reservation.VehicleVersionSnapshot)
            .HasColumnName("vehicle_version_snapshot")
            .IsRequired();

        builder.Property(reservation => reservation.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(reservation => reservation.ConfirmedAtUtc)
            .HasColumnName("confirmed_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(reservation => reservation.ReleasedAtUtc)
            .HasColumnName("released_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.HasOne<Vehicle>()
            .WithMany()
            .HasForeignKey(reservation => reservation.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(reservation => new { reservation.VehicleId, reservation.CreatedAtUtc })
            .HasDatabaseName("ix_vehicle_reservations_vehicle_created");
    }
}

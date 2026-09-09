using AutoSale.Domain.Catalog;
using AutoSale.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoSale.Infrastructure.Persistence.Configurations;

public sealed class CatalogOutboxConfiguration : IEntityTypeConfiguration<CatalogOutbox>
{
    public void Configure(EntityTypeBuilder<CatalogOutbox> builder)
    {
        builder.ToTable("catalog_outbox", table =>
        {
            table.HasCheckConstraint("ck_catalog_outbox_vehicle_version", "vehicle_version > 0");
            table.HasCheckConstraint("ck_catalog_outbox_attempts", "attempts >= 0");
        });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(item => item.VehicleId)
            .HasColumnName("vehicle_id")
            .IsRequired();

        builder.Property(item => item.VehicleVersion)
            .HasColumnName("vehicle_version")
            .IsRequired();

        builder.Property(item => item.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(item => item.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(item => item.ProcessedAtUtc)
            .HasColumnName("processed_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(item => item.Attempts)
            .HasColumnName("attempts")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(item => item.NextAttemptAtUtc)
            .HasColumnName("next_attempt_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(item => item.LastError)
            .HasColumnName("last_error");

        builder.Property(item => item.LeaseOwner)
            .HasColumnName("lease_owner");

        builder.Property(item => item.LeaseExpiresAtUtc)
            .HasColumnName("lease_expires_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.HasOne<Vehicle>()
            .WithMany()
            .HasForeignKey(item => item.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => new { item.VehicleId, item.VehicleVersion })
            .IsUnique()
            .HasDatabaseName("ux_catalog_outbox_vehicle_version");

        builder.HasIndex(item => new { item.ProcessedAtUtc, item.NextAttemptAtUtc, item.CreatedAtUtc })
            .HasDatabaseName("ix_catalog_outbox_pending");

        builder.HasIndex(item => item.LeaseExpiresAtUtc)
            .HasDatabaseName("ix_catalog_outbox_lease_expiration");
    }
}

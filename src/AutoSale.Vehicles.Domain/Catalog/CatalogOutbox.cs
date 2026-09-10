using AutoSale.SharedKernel.Domain;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Catalog;

public sealed class CatalogOutbox : Entity
{
    private CatalogOutbox()
    {
    }

    private CatalogOutbox(Guid id, Guid vehicleId, int vehicleVersion, string payloadJson, DateTimeOffset createdAtUtc)
        : base(id)
    {
        VehicleId = vehicleId;
        VehicleVersion = vehicleVersion;
        PayloadJson = payloadJson;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid VehicleId { get; private set; }

    public int VehicleVersion { get; private set; }

    public string PayloadJson { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset? NextAttemptAtUtc { get; private set; }

    public string? LastError { get; private set; }

    public string? LeaseOwner { get; private set; }

    public DateTimeOffset? LeaseExpiresAtUtc { get; private set; }

    public static Result<CatalogOutbox> Create(Guid vehicleId, int vehicleVersion, string payloadJson, DateTimeOffset now)
    {
        if (vehicleId == Guid.Empty)
        {
            return Result.Failure<CatalogOutbox>(CatalogOutboxErrors.InvalidVehicleId);
        }

        if (vehicleVersion <= 0)
        {
            return Result.Failure<CatalogOutbox>(CatalogOutboxErrors.InvalidVehicleVersion);
        }

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return Result.Failure<CatalogOutbox>(CatalogOutboxErrors.InvalidPayload);
        }

        return Result.Success(new CatalogOutbox(Guid.CreateVersion7(), vehicleId, vehicleVersion, payloadJson, now.ToUniversalTime()));
    }

    public Result AcquireLease(string leaseOwner, DateTimeOffset leaseExpiresAt, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(leaseOwner))
        {
            return Result.Failure(CatalogOutboxErrors.InvalidLeaseOwner);
        }

        var nowUtc = now.ToUniversalTime();
        var leaseExpiresAtUtc = leaseExpiresAt.ToUniversalTime();
        if (leaseExpiresAtUtc <= nowUtc)
        {
            return Result.Failure(CatalogOutboxErrors.InvalidLeaseExpiration);
        }

        if (ProcessedAtUtc is not null || (LeaseExpiresAtUtc > nowUtc && !string.Equals(LeaseOwner, leaseOwner, StringComparison.Ordinal)))
        {
            return Result.Failure(CatalogOutboxErrors.LeaseUnavailable);
        }

        LeaseOwner = leaseOwner.Trim();
        LeaseExpiresAtUtc = leaseExpiresAtUtc;
        Attempts++;
        return Result.Success();
    }

    public void MarkProcessed(DateTimeOffset now)
    {
        ProcessedAtUtc = now.ToUniversalTime();
        NextAttemptAtUtc = null;
        LastError = null;
        ClearLease();
    }

    public void MarkFailed(string? sanitizedError, DateTimeOffset nextAttemptAt)
    {
        LastError = string.IsNullOrWhiteSpace(sanitizedError) ? null : sanitizedError.Trim();
        NextAttemptAtUtc = nextAttemptAt.ToUniversalTime();
        ClearLease();
    }

    private void ClearLease()
    {
        LeaseOwner = null;
        LeaseExpiresAtUtc = null;
    }
}

using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Catalog;

public static class CatalogOutboxErrors
{
    public static readonly Error InvalidVehicleId = new("catalog_outbox.vehicle_id.invalid", "Vehicle id must be a non-empty UUID.", ErrorType.Validation);
    public static readonly Error InvalidVehicleVersion = new("catalog_outbox.vehicle_version.invalid", "Vehicle version must be positive.", ErrorType.Validation);
    public static readonly Error InvalidPayload = new("catalog_outbox.payload.invalid", "Payload JSON is required.", ErrorType.Validation);
    public static readonly Error InvalidLeaseOwner = new("catalog_outbox.lease_owner.invalid", "Lease owner is required.", ErrorType.Validation);
    public static readonly Error InvalidLeaseExpiration = new("catalog_outbox.lease_expiration.invalid", "Lease expiration must be in the future.", ErrorType.Validation);
    public static readonly Error LeaseUnavailable = new("catalog_outbox.lease_unavailable", "The outbox item is leased by another worker.", ErrorType.Conflict);
}

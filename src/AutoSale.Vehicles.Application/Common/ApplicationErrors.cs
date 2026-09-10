using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Common;

public static class ApplicationErrors
{
    public static readonly Error Unauthenticated = new("auth.unauthenticated", "An authenticated user is required to perform this operation.", ErrorType.Unauthorized);
    public static readonly Error InvalidVehicleId = new("vehicle.id.invalid", "Vehicle id must be specified.", ErrorType.Validation);
    public static readonly Error VehicleNotFound = new("vehicle.not_found", "The requested vehicle was not found.", ErrorType.NotFound);
    public static readonly Error InvalidVehicleVersion = new("vehicle.version.invalid", "Vehicle version must be greater than zero.", ErrorType.Validation);
    public static readonly Error InvalidSaleId = new("reservation.sale_id.invalid", "Sale id must be specified.", ErrorType.Validation);
    public static readonly Error InvalidExpectedPrice = new("reservation.expected_price.invalid", "Expected price must be positive and have at most two decimal places.", ErrorType.Validation);
    public static readonly Error ReservationNotFound = new("reservation.not_found", "The requested reservation was not found.", ErrorType.NotFound);
    public static readonly Error ReservationVehicleMismatch = new("reservation.vehicle_mismatch", "The reservation belongs to another vehicle.", ErrorType.Conflict);
    public static readonly Error ReservationRequestMismatch = new("reservation.request_mismatch", "The sale id was previously used with different reservation data.", ErrorType.Conflict);
    public static readonly Error ReservationTerminal = new("reservation_terminal", "A terminal reservation cannot be recreated.", ErrorType.Conflict);
    public static readonly Error InvalidWorkerId = new("catalog.worker_id.invalid", "Worker id is required.", ErrorType.Validation);
    public static readonly Error InvalidBatchSize = new("catalog.batch_size.invalid", "Batch size must be between 1 and 100.", ErrorType.Validation);
    public static readonly Error InvalidLeaseDuration = new("catalog.lease_duration.invalid", "Lease duration must be greater than zero.", ErrorType.Validation);
    public static readonly Error CatalogPublicationFailed = new("catalog.publication_failed", "The catalog destination could not accept the snapshot.", ErrorType.Failure);
    public static readonly Error InvalidPage = new("paging.page.invalid", "Page must be greater than zero.", ErrorType.Validation);
    public static readonly Error InvalidPageSize = new("paging.page_size.invalid", "Page size must be between 1 and 100.", ErrorType.Validation);
}

using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Vehicles;

public static class VehicleErrors
{
    public static readonly Error InvalidMake = new("vehicle.make.invalid", "Make must contain between 1 and 120 characters.", ErrorType.Validation);
    public static readonly Error InvalidModel = new("vehicle.model.invalid", "Model must contain between 1 and 120 characters.", ErrorType.Validation);
    public static readonly Error InvalidYear = new("vehicle.year.invalid", "Year must be between 1886 and the next calendar year.", ErrorType.Validation);
    public static readonly Error InvalidColor = new("vehicle.color.invalid", "Color must contain between 1 and 50 characters.", ErrorType.Validation);
    public static readonly Error InvalidPrice = new("vehicle.price.invalid", "Price must be positive and have at most two decimal places.", ErrorType.Validation);
    public static readonly Error CannotUpdateUnavailableVehicle = new("vehicle.unavailable.cannot_update", "Only an available vehicle can be updated.", ErrorType.Conflict);
    public static readonly Error VersionConflict = new("vehicle.version_conflict", "The vehicle was changed after the supplied version was read.", ErrorType.Conflict);
    public static readonly Error InvalidSaleId = new("vehicle.sale_id.invalid", "Sale id must be a non-empty UUID.", ErrorType.Validation);
    public static readonly Error PriceMismatch = new("vehicle.price_mismatch", "The expected price does not match the current vehicle price.", ErrorType.Conflict);
    public static readonly Error NotAvailable = new("vehicle.not_available", "Only an available vehicle can be reserved.", ErrorType.Conflict);
    public static readonly Error NotReserved = new("vehicle.not_reserved", "The vehicle is not reserved.", ErrorType.Conflict);
    public static readonly Error ReservationOwnerMismatch = new("vehicle.reservation_owner_mismatch", "The reservation belongs to another sale.", ErrorType.Conflict);
    public static readonly Error SoldByAnotherSale = new("vehicle.sold_by_another_sale", "The vehicle was sold by another sale.", ErrorType.Conflict);
    public static readonly Error AlreadySold = new("vehicle.already_sold", "The vehicle has already been sold.", ErrorType.Conflict);
}

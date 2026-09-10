using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Reservations;

public static class ReservationErrors
{
    public static readonly Error InvalidSaleId = new("reservation.sale_id.invalid", "Sale id must be a non-empty UUID.", ErrorType.Validation);
    public static readonly Error InvalidVehicle = new("reservation.vehicle.invalid", "The vehicle must be reserved by this sale before creating its reservation history.", ErrorType.Conflict);
    public static readonly Error AlreadyReleased = new("reservation.released", "A released reservation cannot be confirmed.", ErrorType.Conflict);
    public static readonly Error AlreadyConfirmed = new("reservation.confirmed", "A confirmed reservation cannot be released.", ErrorType.Conflict);
}

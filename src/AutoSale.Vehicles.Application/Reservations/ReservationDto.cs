using AutoSale.Domain.Reservations;

namespace AutoSale.Application.Reservations;

public sealed record ReservationDto(
    Guid SaleId,
    Guid VehicleId,
    ReservationStatus Status,
    string MakeSnapshot,
    string ModelSnapshot,
    int YearSnapshot,
    string ColorSnapshot,
    decimal PriceSnapshot,
    int VehicleVersionSnapshot,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset? ReleasedAtUtc)
{
    public static ReservationDto FromDomain(VehicleReservation reservation) => new(
        reservation.SaleId,
        reservation.VehicleId,
        reservation.Status,
        reservation.MakeSnapshot,
        reservation.ModelSnapshot,
        reservation.YearSnapshot,
        reservation.ColorSnapshot,
        reservation.PriceSnapshot,
        reservation.VehicleVersionSnapshot,
        reservation.CreatedAtUtc,
        reservation.ConfirmedAtUtc,
        reservation.ReleasedAtUtc);
}

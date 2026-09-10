using AutoSale.Application.Reservations;
using AutoSale.Domain.Reservations;

namespace AutoSale.Api.Contracts.Reservations;

public sealed record ReservationDetailsResponse(
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
    public static ReservationDetailsResponse FromDto(ReservationDto reservation) => new(
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

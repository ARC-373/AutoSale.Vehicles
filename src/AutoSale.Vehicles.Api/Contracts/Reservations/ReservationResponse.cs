using AutoSale.Api.Contracts.Vehicles;
using AutoSale.Application.Reservations;
using AutoSale.Domain.Reservations;

namespace AutoSale.Api.Contracts.Reservations;

public sealed record ReservationResponse(
    Guid SaleId,
    ReservationStatus ReservationStatus,
    VehicleSnapshotResponse Vehicle)
{
    public static ReservationResponse FromDto(ReservationResponseDto reservation) => new(
        reservation.SaleId,
        reservation.ReservationStatus,
        VehicleSnapshotResponse.FromDto(reservation.Vehicle));
}

using AutoSale.Domain.Reservations;
using AutoSale.Application.Vehicles;

namespace AutoSale.Application.Reservations;

public sealed record ReservationResponseDto(
    Guid SaleId,
    ReservationStatus ReservationStatus,
    VehicleSnapshotDto Vehicle,
    bool Created);

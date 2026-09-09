using AutoSale.Application.Vehicles;
using AutoSale.Domain.Vehicles;

namespace AutoSale.Api.Contracts.Vehicles;

public sealed record VehicleSnapshotResponse(
    Guid Id,
    string Make,
    string Model,
    int Year,
    string Color,
    decimal Price,
    VehicleStatus Status,
    int Version,
    DateTimeOffset UpdatedAtUtc)
{
    public static VehicleSnapshotResponse FromDto(VehicleSnapshotDto vehicle) => new(
        vehicle.Id,
        vehicle.Make,
        vehicle.Model,
        vehicle.Year,
        vehicle.Color,
        vehicle.Price,
        vehicle.Status,
        vehicle.Version,
        vehicle.UpdatedAtUtc);
}

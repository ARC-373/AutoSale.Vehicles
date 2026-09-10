using AutoSale.Domain.Vehicles;

namespace AutoSale.Application.Vehicles;

public sealed record VehicleSnapshotDto(
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
    public static VehicleSnapshotDto FromDomain(Vehicle vehicle) => new(
        vehicle.Id,
        vehicle.Make,
        vehicle.Model,
        vehicle.Year,
        vehicle.Color,
        vehicle.Price,
        vehicle.Status,
        vehicle.Version,
        vehicle.UpdatedAtUtc);

    public static VehicleSnapshotDto FromDto(VehicleDto vehicle) => new(
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

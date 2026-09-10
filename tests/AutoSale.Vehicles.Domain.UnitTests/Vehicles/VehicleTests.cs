using AutoSale.Domain.Reservations;
using AutoSale.Domain.Vehicles;

namespace AutoSale.Vehicles.Domain.UnitTests.Vehicles;

public sealed class VehicleTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ShouldStartAvailableAtVersionOne()
    {
        var result = Vehicle.Create("Ford", "Mustang", 2025, "Blue", 250_000m, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(VehicleStatus.Available, result.Value!.Status);
        Assert.Equal(1, result.Value.Version);
        Assert.Null(result.Value.ReservationSaleId);
        Assert.Null(result.Value.SoldSaleId);
    }

    [Fact]
    public void Reserve_ShouldCaptureTheAcceptedVehicleVersion()
    {
        var vehicle = CreateVehicle();
        var saleId = Guid.NewGuid();

        var reserve = vehicle.Reserve(saleId, vehicle.Price, Now.AddMinutes(1));
        var reservation = VehicleReservation.Create(saleId, vehicle, Now.AddMinutes(1));

        Assert.True(reserve.IsSuccess);
        Assert.True(reservation.IsSuccess);
        Assert.Equal(VehicleStatus.Reserved, vehicle.Status);
        Assert.Equal(2, vehicle.Version);
        Assert.Equal(vehicle.Version, reservation.Value!.VehicleVersionSnapshot);
        Assert.Equal(vehicle.Price, reservation.Value.PriceSnapshot);
    }

    [Fact]
    public void Update_ShouldFailWhileVehicleIsReserved()
    {
        var vehicle = CreateVehicle();
        vehicle.Reserve(Guid.NewGuid(), vehicle.Price, Now.AddMinutes(1));

        var update = vehicle.UpdateDetails("Ford", "Mustang GT", 2026, "Black", 300_000m, vehicle.Version, Now.AddMinutes(2));

        Assert.True(update.IsFailure);
        Assert.Equal(VehicleErrors.CannotUpdateUnavailableVehicle, update.Error);
    }

    [Fact]
    public void Confirm_ShouldBeIdempotentForTheOwningSale()
    {
        var vehicle = CreateVehicle();
        var saleId = Guid.NewGuid();
        vehicle.Reserve(saleId, vehicle.Price, Now.AddMinutes(1));

        var first = vehicle.ConfirmSale(saleId, Now.AddMinutes(2));
        var confirmedVersion = vehicle.Version;
        var repeated = vehicle.ConfirmSale(saleId, Now.AddMinutes(3));

        Assert.True(first.IsSuccess);
        Assert.True(repeated.IsSuccess);
        Assert.Equal(confirmedVersion, vehicle.Version);
        Assert.Equal(VehicleStatus.Sold, vehicle.Status);
        Assert.Equal(saleId, vehicle.SoldSaleId);
        Assert.Null(vehicle.ReservationSaleId);
    }

    [Fact]
    public void OldSale_ShouldNotReleaseANewerReservation()
    {
        var vehicle = CreateVehicle();
        var oldSaleId = Guid.NewGuid();
        var newSaleId = Guid.NewGuid();
        vehicle.Reserve(oldSaleId, vehicle.Price, Now.AddMinutes(1));
        vehicle.ReleaseReservation(oldSaleId, Now.AddMinutes(2));
        vehicle.Reserve(newSaleId, vehicle.Price, Now.AddMinutes(3));

        var release = vehicle.ReleaseReservation(oldSaleId, Now.AddMinutes(4));

        Assert.True(release.IsFailure);
        Assert.Equal(VehicleErrors.ReservationOwnerMismatch, release.Error);
        Assert.Equal(VehicleStatus.Reserved, vehicle.Status);
        Assert.Equal(newSaleId, vehicle.ReservationSaleId);
    }

    private static Vehicle CreateVehicle() =>
        Vehicle.Create("Ford", "Mustang", 2025, "Blue", 250_000m, Now).Value!;
}

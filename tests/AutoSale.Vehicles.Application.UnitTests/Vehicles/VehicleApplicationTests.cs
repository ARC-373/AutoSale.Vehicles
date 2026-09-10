using AutoSale.Application.Vehicles.Create;
using AutoSale.Application.Vehicles.ReleaseReservation;
using AutoSale.Application.Vehicles.Reserve;
using AutoSale.Application.Vehicles.Update;
using AutoSale.Domain.Reservations;
using AutoSale.Domain.Vehicles;
using AutoSale.Vehicles.Application.UnitTests.Fakes;

namespace AutoSale.Vehicles.Application.UnitTests.Vehicles;

public sealed class VehicleApplicationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_ShouldPersistVehicleAndVersionedOutboxTogether()
    {
        var vehicles = new FakeVehicleRepository();
        var outbox = new FakeCatalogOutboxRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateVehicleHandler(vehicles, outbox, unitOfWork, new TestClock(Now));

        var result = await handler.HandleAsync(
            new CreateVehicleCommand("Toyota", "Corolla", 2025, "Silver", 145_000m),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(vehicles.Vehicles);
        var message = Assert.Single(outbox.Items);
        Assert.Equal(result.Value!.Id, message.VehicleId);
        Assert.Equal(result.Value.Version, message.VehicleVersion);
        Assert.Contains("\"status\":\"Available\"", message.PayloadJson, StringComparison.Ordinal);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Update_ShouldRejectAStaleExplicitVersion()
    {
        var vehicle = CreateVehicle();
        var vehicles = new FakeVehicleRepository();
        vehicles.Vehicles.Add(vehicle.Id, vehicle);
        var outbox = new FakeCatalogOutboxRepository();
        var handler = new UpdateVehicleHandler(vehicles, outbox, new FakeUnitOfWork(), new TestClock(Now));

        var result = await handler.HandleAsync(
            new UpdateVehicleCommand(vehicle.Id, "Toyota", "Corolla XEi", 2025, "Black", 150_000m, 99),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(VehicleErrors.VersionConflict, result.Error);
        Assert.Empty(outbox.Items);
    }

    [Fact]
    public async Task Reserve_ShouldCreateHistoryAndOutboxInOneCommit()
    {
        var vehicle = CreateVehicle();
        var vehicles = new FakeVehicleRepository();
        vehicles.Vehicles.Add(vehicle.Id, vehicle);
        var reservations = new FakeReservationRepository();
        var outbox = new FakeCatalogOutboxRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ReserveVehicleHandler(
            vehicles,
            reservations,
            outbox,
            unitOfWork,
            new TestClock(Now));
        var saleId = Guid.NewGuid();

        var result = await handler.HandleAsync(
            new ReserveVehicleCommand(vehicle.Id, saleId, vehicle.Price),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Created);
        Assert.Equal(VehicleStatus.Reserved, vehicle.Status);
        Assert.True(reservations.Reservations.ContainsKey(saleId));
        Assert.Single(outbox.Items);
        Assert.Equal(1, unitOfWork.Commits);
    }

    [Fact]
    public async Task RepeatedEquivalentReservation_ShouldReturnExistingWithoutAnotherMutation()
    {
        var vehicle = CreateVehicle();
        var vehicles = new FakeVehicleRepository();
        vehicles.Vehicles.Add(vehicle.Id, vehicle);
        var reservations = new FakeReservationRepository();
        var outbox = new FakeCatalogOutboxRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ReserveVehicleHandler(
            vehicles,
            reservations,
            outbox,
            unitOfWork,
            new TestClock(Now));
        var saleId = Guid.NewGuid();
        var command = new ReserveVehicleCommand(vehicle.Id, saleId, vehicle.Price);

        await handler.HandleAsync(command, CancellationToken.None);
        var repeated = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(repeated.IsSuccess);
        Assert.False(repeated.Value!.Created);
        Assert.Single(reservations.Reservations);
        Assert.Single(outbox.Items);
        Assert.Equal(2, vehicle.Version);
        Assert.Equal(1, unitOfWork.Commits);
    }

    [Fact]
    public async Task RepeatedOldRelease_ShouldNotChangeANewerReservation()
    {
        var vehicle = CreateVehicle();
        var oldSaleId = Guid.NewGuid();
        vehicle.Reserve(oldSaleId, vehicle.Price, Now);
        var oldReservation = VehicleReservation.Create(oldSaleId, vehicle, Now).Value!;
        vehicle.ReleaseReservation(oldSaleId, Now.AddMinutes(1));
        oldReservation.Release(Now.AddMinutes(1));
        var newSaleId = Guid.NewGuid();
        vehicle.Reserve(newSaleId, vehicle.Price, Now.AddMinutes(2));

        var vehicles = new FakeVehicleRepository();
        vehicles.Vehicles.Add(vehicle.Id, vehicle);
        var reservations = new FakeReservationRepository();
        reservations.Reservations.Add(oldSaleId, oldReservation);
        var outbox = new FakeCatalogOutboxRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ReleaseVehicleReservationHandler(
            vehicles,
            reservations,
            outbox,
            unitOfWork,
            new TestClock(Now.AddMinutes(3)));

        var result = await handler.HandleAsync(
            new ReleaseVehicleReservationCommand(vehicle.Id, oldSaleId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(VehicleStatus.Reserved, result.Value!.Status);
        Assert.Equal(newSaleId, vehicle.ReservationSaleId);
        Assert.Empty(outbox.Items);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    private static Vehicle CreateVehicle() =>
        Vehicle.Create("Toyota", "Corolla", 2025, "Silver", 145_000m, Now).Value!;
}

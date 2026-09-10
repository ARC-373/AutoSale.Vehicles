using AutoSale.Application.Vehicles.ConfirmSale;
using AutoSale.Application.Vehicles.ReleaseReservation;
using AutoSale.Domain.Reservations;
using AutoSale.Domain.Vehicles;
using AutoSale.Vehicles.Application.UnitTests.Fakes;

namespace AutoSale.Vehicles.Application.UnitTests.Vehicles;

public sealed class ReservationLifecycleHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Confirm_ShouldFinalizeVehicleReservationAndOutboxInOneCommit()
    {
        var fixture = CreateReservedVehicle();
        var outbox = new FakeCatalogOutboxRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ConfirmVehicleSaleHandler(
            fixture.Vehicles,
            fixture.Reservations,
            outbox,
            unitOfWork,
            new TestClock(Now.AddMinutes(1)));

        var result = await handler.HandleAsync(
            new ConfirmVehicleSaleCommand(fixture.Vehicle.Id, fixture.SaleId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(VehicleStatus.Sold, fixture.Vehicle.Status);
        Assert.Equal(ReservationStatus.Confirmed, fixture.Reservation.Status);
        Assert.Equal(fixture.SaleId, fixture.Vehicle.SoldSaleId);
        Assert.Equal(3, Assert.Single(outbox.Items).VehicleVersion);
        Assert.Equal(1, unitOfWork.Commits);
    }

    [Fact]
    public async Task Release_ShouldMakeVehicleAvailableAndRecordTerminalHistory()
    {
        var fixture = CreateReservedVehicle();
        var outbox = new FakeCatalogOutboxRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ReleaseVehicleReservationHandler(
            fixture.Vehicles,
            fixture.Reservations,
            outbox,
            unitOfWork,
            new TestClock(Now.AddMinutes(1)));

        var result = await handler.HandleAsync(
            new ReleaseVehicleReservationCommand(fixture.Vehicle.Id, fixture.SaleId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(VehicleStatus.Available, fixture.Vehicle.Status);
        Assert.Equal(ReservationStatus.Released, fixture.Reservation.Status);
        Assert.Null(fixture.Vehicle.ReservationSaleId);
        Assert.Equal(3, Assert.Single(outbox.Items).VehicleVersion);
        Assert.Equal(1, unitOfWork.Commits);
    }

    private static ReservationFixture CreateReservedVehicle()
    {
        var vehicle = Vehicle.Create("Honda", "Civic", 2025, "Black", 150_000m, Now).Value!;
        var saleId = Guid.NewGuid();
        vehicle.Reserve(saleId, vehicle.Price, Now);
        var reservation = VehicleReservation.Create(saleId, vehicle, Now).Value!;
        var vehicles = new FakeVehicleRepository();
        vehicles.Vehicles.Add(vehicle.Id, vehicle);
        var reservations = new FakeReservationRepository();
        reservations.Reservations.Add(saleId, reservation);
        return new ReservationFixture(vehicle, reservation, saleId, vehicles, reservations);
    }

    private sealed record ReservationFixture(
        Vehicle Vehicle,
        VehicleReservation Reservation,
        Guid SaleId,
        FakeVehicleRepository Vehicles,
        FakeReservationRepository Reservations);
}

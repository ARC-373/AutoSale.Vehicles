using AutoSale.Domain.Reservations;
using AutoSale.Domain.Vehicles;

namespace AutoSale.Vehicles.Domain.UnitTests.Reservations;

public sealed class VehicleReservationTests
{
    [Fact]
    public void ReleasedReservation_ShouldBeTerminalAndIdempotent()
    {
        var now = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        var vehicle = Vehicle.Create("Honda", "Civic", 2024, "White", 150_000m, now).Value!;
        var saleId = Guid.NewGuid();
        vehicle.Reserve(saleId, vehicle.Price, now);
        var reservation = VehicleReservation.Create(saleId, vehicle, now).Value!;

        var firstRelease = reservation.Release(now.AddMinutes(1));
        var repeatedRelease = reservation.Release(now.AddMinutes(2));
        var confirm = reservation.Confirm(now.AddMinutes(3));

        Assert.True(firstRelease.IsSuccess);
        Assert.True(repeatedRelease.IsSuccess);
        Assert.True(confirm.IsFailure);
        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.Equal(now.AddMinutes(1), reservation.ReleasedAtUtc);
        Assert.Null(reservation.ConfirmedAtUtc);
    }
}

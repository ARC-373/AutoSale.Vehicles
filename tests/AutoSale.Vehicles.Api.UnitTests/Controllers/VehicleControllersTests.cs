using AutoSale.Api.Contracts.Reservations;
using AutoSale.Api.Contracts.Vehicles;
using AutoSale.Api.Controllers;
using AutoSale.Application.Reservations;
using AutoSale.Application.Vehicles;
using AutoSale.Application.Vehicles.Create;
using AutoSale.Application.Vehicles.Reserve;
using AutoSale.Domain.Reservations;
using AutoSale.Domain.Vehicles;
using AutoSale.SharedKernel.Results;
using AutoSale.Vehicles.Api.UnitTests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AutoSale.Vehicles.Api.UnitTests.Controllers;

public sealed class VehicleControllersTests
{
    [Fact]
    public async Task Create_ShouldReturnLocationForAdministrativeLookup()
    {
        var vehicleId = Guid.NewGuid();
        var dto = new VehicleDto(
            vehicleId,
            "Honda",
            "Civic",
            2025,
            "Black",
            150_000m,
            VehicleStatus.Available,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            1);
        var handler = new StubCommandHandler<CreateVehicleCommand, Result<VehicleDto>>(_ => Result.Success(dto));
        var controller = WithHttpContext(new VehiclesController());

        var result = await controller.CreateAsync(
            new CreateVehicleRequest("Honda", "Civic", 2025, "Black", 150_000m),
            handler,
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(VehiclesController.GetByIdAsync), created.ActionName);
        Assert.Equal(vehicleId, created.RouteValues!["id"]);
        Assert.IsType<VehicleResponse>(created.Value);
    }

    [Theory]
    [InlineData(true, StatusCodes.Status201Created)]
    [InlineData(false, StatusCodes.Status200OK)]
    public async Task Reserve_ShouldReflectWhetherTheReservationWasCreated(bool created, int expectedStatus)
    {
        var vehicleId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var response = new ReservationResponseDto(
            saleId,
            ReservationStatus.Reserved,
            new VehicleSnapshotDto(
                vehicleId,
                "Honda",
                "Civic",
                2025,
                "Black",
                150_000m,
                VehicleStatus.Reserved,
                2,
                DateTimeOffset.UtcNow),
            created);
        var handler = new StubCommandHandler<ReserveVehicleCommand, Result<ReservationResponseDto>>(
            _ => Result.Success(response));
        var controller = WithHttpContext(new InternalVehicleReservationsController());

        var result = await controller.ReserveAsync(
            vehicleId,
            saleId,
            new ReserveVehicleRequest(150_000m),
            handler,
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
        Assert.IsType<ReservationResponse>(objectResult.Value);
    }

    private static TController WithHttpContext<TController>(TController controller)
        where TController : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }
}

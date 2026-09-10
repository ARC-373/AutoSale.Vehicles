using AutoSale.Api.Authorization;
using AutoSale.Api.Contracts.Reservations;
using AutoSale.Api.Contracts.Vehicles;
using AutoSale.Api.Extensions;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Reservations;
using AutoSale.Application.Vehicles;
using AutoSale.Application.Vehicles.ConfirmSale;
using AutoSale.Application.Vehicles.ReleaseReservation;
using AutoSale.Application.Vehicles.Reserve;
using AutoSale.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoSale.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.InternalSales)]
[Route("internal/v1/vehicles/{vehicleId:guid}/reservations/{saleId:guid}")]
public sealed class InternalVehicleReservationsController : ControllerBase
{
    [HttpPut]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> ReserveAsync(
        Guid vehicleId,
        Guid saleId,
        [FromBody] ReserveVehicleRequest request,
        [FromServices] ICommandHandler<ReserveVehicleCommand, Result<ReservationResponseDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ReserveVehicleCommand(vehicleId, saleId, request.ExpectedPrice),
            cancellationToken);
        if (result.IsFailure)
        {
            return ResultExtensions.ToProblem(result.Error, this);
        }

        return new ObjectResult(ReservationResponse.FromDto(result.Value!))
        {
            StatusCode = result.Value!.Created
                ? StatusCodes.Status201Created
                : StatusCodes.Status200OK
        };
    }

    [HttpPut("confirmation")]
    [ProducesResponseType<VehicleSnapshotResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleSnapshotResponse>> ConfirmAsync(
        Guid vehicleId,
        Guid saleId,
        [FromServices] ICommandHandler<ConfirmVehicleSaleCommand, Result<VehicleSnapshotDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ConfirmVehicleSaleCommand(vehicleId, saleId), cancellationToken);
        return result.ToActionResult(this, VehicleSnapshotResponse.FromDto);
    }

    [HttpPut("release")]
    [ProducesResponseType<VehicleSnapshotResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleSnapshotResponse>> ReleaseAsync(
        Guid vehicleId,
        Guid saleId,
        [FromServices] ICommandHandler<ReleaseVehicleReservationCommand, Result<VehicleSnapshotDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ReleaseVehicleReservationCommand(vehicleId, saleId), cancellationToken);
        return result.ToActionResult(this, VehicleSnapshotResponse.FromDto);
    }
}

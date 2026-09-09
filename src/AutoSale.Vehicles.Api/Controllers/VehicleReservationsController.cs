using AutoSale.Api.Authorization;
using AutoSale.Api.Contracts.Common;
using AutoSale.Api.Contracts.Reservations;
using AutoSale.Api.Extensions;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Common;
using AutoSale.Application.Reservations;
using AutoSale.Application.Vehicles.ListReservations;
using AutoSale.Application.Vehicles.ListVehicleReservations;
using AutoSale.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoSale.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class VehicleReservationsController : ControllerBase
{
    [HttpGet("api/v1/reservations")]
    [ProducesResponseType<PagedResponse<ReservationDetailsResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<ReservationDetailsResponse>>> ListAsync(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] IQueryHandler<ListReservationsQuery, Result<PagedResult<ReservationDto>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListReservationsQuery(page ?? 1, pageSize ?? 20),
            cancellationToken);
        return result.ToActionResult(
            this,
            items => PagedResponse<ReservationDetailsResponse>.From(items, ReservationDetailsResponse.FromDto));
    }

    [HttpGet("api/v1/vehicles/{vehicleId:guid}/reservations")]
    [ProducesResponseType<PagedResponse<ReservationDetailsResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<ReservationDetailsResponse>>> ListByVehicleAsync(
        Guid vehicleId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] IQueryHandler<ListVehicleReservationsQuery, Result<PagedResult<ReservationDto>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListVehicleReservationsQuery(vehicleId, page ?? 1, pageSize ?? 20),
            cancellationToken);
        return result.ToActionResult(
            this,
            items => PagedResponse<ReservationDetailsResponse>.From(items, ReservationDetailsResponse.FromDto));
    }
}

using AutoSale.Api.Authorization;
using AutoSale.Api.Contracts.Common;
using AutoSale.Api.Contracts.Vehicles;
using AutoSale.Api.Extensions;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Common;
using AutoSale.Application.Vehicles;
using AutoSale.Application.Vehicles.Create;
using AutoSale.Application.Vehicles.ListAvailable;
using AutoSale.Application.Vehicles.Update;
using AutoSale.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoSale.Api.Controllers;

[ApiController]
[Route("api/v1/vehicles")]
public sealed class VehiclesController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType<VehicleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VehicleResponse>> CreateAsync(
        [FromBody] CreateVehicleRequest request,
        [FromServices] ICommandHandler<CreateVehicleCommand, Result<VehicleDto>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateVehicleCommand(request.Make, request.Model, request.Year, request.Color, request.Price);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, VehicleResponse.FromDto, StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType<VehicleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleResponse>> UpdateAsync(
        Guid id,
        [FromBody] UpdateVehicleRequest request,
        [FromServices] ICommandHandler<UpdateVehicleCommand, Result<VehicleDto>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateVehicleCommand(id, request.Make, request.Model, request.Year, request.Color, request.Price);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, VehicleResponse.FromDto);
    }

    [HttpGet("available")]
    [AllowAnonymous]
    [ProducesResponseType<PagedResponse<VehicleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<VehicleResponse>>> ListAvailableAsync(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] IQueryHandler<ListAvailableVehiclesQuery, Result<PagedResult<VehicleDto>>> handler,
        CancellationToken cancellationToken)
    {
        var query = new ListAvailableVehiclesQuery(page ?? 1, pageSize ?? 20);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToActionResult(this, pageResult => PagedResponse<VehicleResponse>.From(pageResult, VehicleResponse.FromDto));
    }
}

using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Vehicles.List;

public sealed class ListVehiclesHandler : IQueryHandler<ListVehiclesQuery, Result<PagedResult<VehicleDto>>>
{
    private readonly IVehicleRepository _vehicleRepository;

    public ListVehiclesHandler(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    public async Task<Result<PagedResult<VehicleDto>>> HandleAsync(ListVehiclesQuery query, CancellationToken cancellationToken)
    {
        var validation = PagingValidator.Validate(query.Page, query.PageSize);
        if (validation.IsFailure)
        {
            return Result.Failure<PagedResult<VehicleDto>>(validation.Error);
        }

        return Result.Success(await _vehicleRepository.ListAsync(query.Page, query.PageSize, cancellationToken));
    }
}

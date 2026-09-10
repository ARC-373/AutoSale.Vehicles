using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Vehicles.GetById;

public sealed class GetVehicleByIdHandler : IQueryHandler<GetVehicleByIdQuery, Result<VehicleDto>>
{
    private readonly IVehicleRepository _vehicleRepository;

    public GetVehicleByIdHandler(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    public async Task<Result<VehicleDto>> HandleAsync(GetVehicleByIdQuery query, CancellationToken cancellationToken)
    {
        if (query.VehicleId == Guid.Empty)
        {
            return Result.Failure<VehicleDto>(ApplicationErrors.InvalidVehicleId);
        }

        var vehicle = await _vehicleRepository.GetByIdAsync(query.VehicleId, cancellationToken);
        return vehicle is null
            ? Result.Failure<VehicleDto>(ApplicationErrors.VehicleNotFound)
            : Result.Success(VehicleDto.FromDomain(vehicle));
    }
}

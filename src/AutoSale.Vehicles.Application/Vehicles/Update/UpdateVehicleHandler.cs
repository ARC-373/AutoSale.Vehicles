using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Vehicles.Update;

public sealed class UpdateVehicleHandler : ICommandHandler<UpdateVehicleCommand, Result<VehicleDto>>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateVehicleHandler(IVehicleRepository vehicleRepository, IUnitOfWork unitOfWork, IClock clock)
    {
        _vehicleRepository = vehicleRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<VehicleDto>> HandleAsync(UpdateVehicleCommand command, CancellationToken cancellationToken)
    {
        var validation = UpdateVehicleValidator.Validate(command);
        if (validation.IsFailure)
        {
            return Result.Failure<VehicleDto>(validation.Error);
        }

        var vehicle = await _vehicleRepository.GetByIdAsync(command.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return Result.Failure<VehicleDto>(ApplicationErrors.VehicleNotFound);
        }

        var update = vehicle.UpdateDetails(command.Make, command.Model, command.Year, command.Color, command.Price, _clock.UtcNow);
        if (update.IsFailure)
        {
            return Result.Failure<VehicleDto>(update.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(VehicleDto.FromDomain(vehicle));
    }
}

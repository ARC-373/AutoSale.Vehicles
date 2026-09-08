using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Domain.Vehicles;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Vehicles.Create;

public sealed class CreateVehicleHandler : ICommandHandler<CreateVehicleCommand, Result<VehicleDto>>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateVehicleHandler(IVehicleRepository vehicleRepository, IUnitOfWork unitOfWork, IClock clock)
    {
        _vehicleRepository = vehicleRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<VehicleDto>> HandleAsync(CreateVehicleCommand command, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var creation = CreateVehicleValidator.Validate(command, now);
        if (creation.IsFailure)
        {
            return Result.Failure<VehicleDto>(creation.Error);
        }

        var vehicle = creation.Value!;
        await _vehicleRepository.AddAsync(vehicle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(VehicleDto.FromDomain(vehicle));
    }
}

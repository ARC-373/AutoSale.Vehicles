using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Catalog;
using AutoSale.Domain.Vehicles;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Vehicles.Create;

public sealed class CreateVehicleHandler : ICommandHandler<CreateVehicleCommand, Result<VehicleDto>>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ICatalogOutboxRepository _catalogOutboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateVehicleHandler(
        IVehicleRepository vehicleRepository,
        ICatalogOutboxRepository catalogOutboxRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _vehicleRepository = vehicleRepository;
        _catalogOutboxRepository = catalogOutboxRepository;
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
        var outbox = CatalogOutboxFactory.Create(vehicle, now);
        if (outbox.IsFailure)
        {
            return Result.Failure<VehicleDto>(outbox.Error);
        }

        await _vehicleRepository.AddAsync(vehicle, cancellationToken);
        await _catalogOutboxRepository.AddAsync(outbox.Value!, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(VehicleDto.FromDomain(vehicle));
    }
}

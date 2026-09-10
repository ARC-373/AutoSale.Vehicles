using System.Data;
using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.Application.Catalog;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Vehicles.Update;

public sealed class UpdateVehicleHandler : ICommandHandler<UpdateVehicleCommand, Result<VehicleDto>>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ICatalogOutboxRepository _catalogOutboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateVehicleHandler(
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

    public async Task<Result<VehicleDto>> HandleAsync(UpdateVehicleCommand command, CancellationToken cancellationToken)
    {
        var validation = UpdateVehicleValidator.Validate(command);
        if (validation.IsFailure)
        {
            return Result.Failure<VehicleDto>(validation.Error);
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var vehicle = await _vehicleRepository.GetByIdForUpdateAsync(command.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return Result.Failure<VehicleDto>(ApplicationErrors.VehicleNotFound);
        }

        var now = _clock.UtcNow;
        var update = vehicle.UpdateDetails(command.Make, command.Model, command.Year, command.Color, command.Price, command.Version, now);
        if (update.IsFailure)
        {
            return Result.Failure<VehicleDto>(update.Error);
        }

        var outbox = CatalogOutboxFactory.Create(vehicle, now);
        if (outbox.IsFailure)
        {
            return Result.Failure<VehicleDto>(outbox.Error);
        }

        await _catalogOutboxRepository.AddAsync(outbox.Value!, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success(VehicleDto.FromDomain(vehicle));
    }
}

using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.Application.Vehicles;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Catalog.Rebuild;

public sealed class RebuildCatalogHandler : ICommandHandler<RebuildCatalogCommand, Result<int>>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ICatalogOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RebuildCatalogHandler(
        IVehicleRepository vehicleRepository,
        ICatalogOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _vehicleRepository = vehicleRepository;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<int>> HandleAsync(RebuildCatalogCommand command, CancellationToken cancellationToken)
    {
        if (command.BatchSize is < 1 or > 100)
        {
            return Result.Failure<int>(ApplicationErrors.InvalidBatchSize);
        }

        var page = 1;
        var created = 0;

        while (true)
        {
            var vehicles = await _vehicleRepository.ListAsync(page, command.BatchSize, cancellationToken);
            foreach (var vehicle in vehicles.Items)
            {
                var existing = await _outboxRepository.GetByVehicleVersionAsync(
                    vehicle.Id,
                    vehicle.Version,
                    cancellationToken);
                if (existing is not null)
                {
                    continue;
                }

                var outbox = CatalogOutboxFactory.Create(VehicleSnapshotDto.FromDto(vehicle), _clock.UtcNow);
                if (outbox.IsFailure)
                {
                    return Result.Failure<int>(outbox.Error);
                }

                await _outboxRepository.AddAsync(outbox.Value!, cancellationToken);
                created++;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (page >= vehicles.TotalPages)
            {
                break;
            }

            page++;
        }

        return Result.Success(created);
    }
}

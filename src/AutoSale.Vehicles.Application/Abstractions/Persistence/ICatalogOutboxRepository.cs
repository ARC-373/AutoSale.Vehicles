using AutoSale.Domain.Catalog;

namespace AutoSale.Application.Abstractions.Persistence;

public interface ICatalogOutboxRepository
{
    Task AddAsync(CatalogOutbox item, CancellationToken cancellationToken);

    Task<CatalogOutbox?> GetByVehicleVersionAsync(Guid vehicleId, int vehicleVersion, CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogOutbox>> GetPendingAsync(DateTimeOffset nowUtc, int batchSize, CancellationToken cancellationToken);
}

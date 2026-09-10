using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace AutoSale.Infrastructure.Persistence.Repositories;

public sealed class CatalogOutboxRepository : ICatalogOutboxRepository
{
    private readonly AutoSaleDbContext _dbContext;

    public CatalogOutboxRepository(AutoSaleDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(CatalogOutbox item, CancellationToken cancellationToken)
    {
        await _dbContext.CatalogOutbox.AddAsync(item, cancellationToken);
    }

    public Task<CatalogOutbox?> GetByVehicleVersionAsync(
        Guid vehicleId,
        int vehicleVersion,
        CancellationToken cancellationToken) =>
        _dbContext.CatalogOutbox.SingleOrDefaultAsync(
            item => item.VehicleId == vehicleId && item.VehicleVersion == vehicleVersion,
            cancellationToken);

    public async Task<IReadOnlyList<CatalogOutbox>> GetPendingAsync(
        DateTimeOffset nowUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var utc = nowUtc.ToUniversalTime();
        return await _dbContext.CatalogOutbox
            .FromSqlInterpolated($$"""
                SELECT *
                FROM catalog_outbox
                WHERE processed_at_utc IS NULL
                  AND (next_attempt_at_utc IS NULL OR next_attempt_at_utc <= {{utc}})
                  AND (lease_expires_at_utc IS NULL OR lease_expires_at_utc <= {{utc}})
                ORDER BY created_at_utc, id
                LIMIT {{batchSize}}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);
    }
}

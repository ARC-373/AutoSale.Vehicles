using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.Domain.Reservations;
using Microsoft.EntityFrameworkCore;

namespace AutoSale.Infrastructure.Persistence.Repositories;

public sealed class ReservationRepository : IReservationRepository
{
    private readonly AutoSaleDbContext _dbContext;

    public ReservationRepository(AutoSaleDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<VehicleReservation?> GetBySaleIdAsync(Guid saleId, CancellationToken cancellationToken) =>
        _dbContext.VehicleReservations.SingleOrDefaultAsync(reservation => reservation.SaleId == saleId, cancellationToken);

    public Task<VehicleReservation?> GetBySaleIdForUpdateAsync(Guid saleId, CancellationToken cancellationToken) =>
        _dbContext.VehicleReservations
            .FromSqlInterpolated($"SELECT * FROM vehicle_reservations WHERE sale_id = {saleId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    public async Task AddAsync(VehicleReservation reservation, CancellationToken cancellationToken)
    {
        await _dbContext.VehicleReservations.AddAsync(reservation, cancellationToken);
    }

    public async Task<PagedResult<VehicleReservation>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _dbContext.VehicleReservations.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(reservation => reservation.CreatedAtUtc)
            .ThenBy(reservation => reservation.SaleId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<VehicleReservation>(items, page, pageSize, totalCount);
    }

    public async Task<PagedResult<VehicleReservation>> ListByVehicleIdAsync(
        Guid vehicleId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.VehicleReservations
            .AsNoTracking()
            .Where(reservation => reservation.VehicleId == vehicleId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(reservation => reservation.CreatedAtUtc)
            .ThenBy(reservation => reservation.SaleId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<VehicleReservation>(items, page, pageSize, totalCount);
    }
}

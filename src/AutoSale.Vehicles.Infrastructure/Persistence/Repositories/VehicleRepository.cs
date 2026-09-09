using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.Application.Vehicles;
using AutoSale.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace AutoSale.Infrastructure.Persistence.Repositories;

public sealed class VehicleRepository : IVehicleRepository
{
    private readonly AutoSaleDbContext _dbContext;

    public VehicleRepository(AutoSaleDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Vehicles.SingleOrDefaultAsync(vehicle => vehicle.Id == id, cancellationToken);

    public Task<Vehicle?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Vehicles
            .FromSqlInterpolated($"SELECT * FROM vehicles WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        await _dbContext.Vehicles.AddAsync(vehicle, cancellationToken);
    }

    public Task<PagedResult<VehicleDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        ListAsync(_dbContext.Vehicles.AsNoTracking(), page, pageSize, cancellationToken);

    private static async Task<PagedResult<VehicleDto>> ListAsync(
        IQueryable<Vehicle> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(vehicle => vehicle.Price)
            .ThenBy(vehicle => vehicle.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(vehicle => new VehicleDto(
                vehicle.Id,
                vehicle.Make,
                vehicle.Model,
                vehicle.Year,
                vehicle.Color,
                vehicle.Price,
                vehicle.Status,
                vehicle.CreatedAtUtc,
                vehicle.UpdatedAtUtc,
                vehicle.Version))
            .ToListAsync(cancellationToken);

        return new PagedResult<VehicleDto>(items, page, pageSize, totalCount);
    }
}

using AutoSale.Application.Common;
using AutoSale.Application.Vehicles;
using AutoSale.Domain.Vehicles;

namespace AutoSale.Application.Abstractions.Persistence;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Vehicle?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken);

    Task<PagedResult<VehicleDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}

using AutoSale.Application.Common;
using AutoSale.Domain.Reservations;

namespace AutoSale.Application.Abstractions.Persistence;

public interface IReservationRepository
{
    Task<VehicleReservation?> GetBySaleIdAsync(Guid saleId, CancellationToken cancellationToken);

    Task<VehicleReservation?> GetBySaleIdForUpdateAsync(Guid saleId, CancellationToken cancellationToken);

    Task AddAsync(VehicleReservation reservation, CancellationToken cancellationToken);

    Task<PagedResult<VehicleReservation>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<PagedResult<VehicleReservation>> ListByVehicleIdAsync(Guid vehicleId, int page, int pageSize, CancellationToken cancellationToken);
}

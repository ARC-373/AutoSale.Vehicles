using System.Data;
using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Integrations;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.Application.Vehicles;
using AutoSale.Domain.Catalog;
using AutoSale.Domain.Reservations;
using AutoSale.Domain.Vehicles;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Vehicles.Application.UnitTests.Fakes;

internal sealed class TestClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;
}

internal sealed class FakeVehicleRepository : IVehicleRepository
{
    public Dictionary<Guid, Vehicle> Vehicles { get; } = [];

    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Vehicles.GetValueOrDefault(id));

    public Task<Vehicle?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        GetByIdAsync(id, cancellationToken);

    public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        Vehicles.Add(vehicle.Id, vehicle);
        return Task.CompletedTask;
    }

    public Task<PagedResult<VehicleDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        ListAsync(Vehicles.Values, page, pageSize);

    private static Task<PagedResult<VehicleDto>> ListAsync(IEnumerable<Vehicle> source, int page, int pageSize)
    {
        var all = source.OrderBy(vehicle => vehicle.Price).ThenBy(vehicle => vehicle.Id).ToArray();
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).Select(VehicleDto.FromDomain).ToArray();
        return Task.FromResult(new PagedResult<VehicleDto>(items, page, pageSize, all.Length));
    }
}

internal sealed class FakeReservationRepository : IReservationRepository
{
    public Dictionary<Guid, VehicleReservation> Reservations { get; } = [];

    public Task<VehicleReservation?> GetBySaleIdAsync(Guid saleId, CancellationToken cancellationToken) =>
        Task.FromResult(Reservations.GetValueOrDefault(saleId));

    public Task<VehicleReservation?> GetBySaleIdForUpdateAsync(Guid saleId, CancellationToken cancellationToken) =>
        GetBySaleIdAsync(saleId, cancellationToken);

    public Task AddAsync(VehicleReservation reservation, CancellationToken cancellationToken)
    {
        Reservations.Add(reservation.SaleId, reservation);
        return Task.CompletedTask;
    }

    public Task<PagedResult<VehicleReservation>> ListAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        Page(Reservations.Values, page, pageSize);

    public Task<PagedResult<VehicleReservation>> ListByVehicleIdAsync(
        Guid vehicleId,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        Page(Reservations.Values.Where(reservation => reservation.VehicleId == vehicleId), page, pageSize);

    private static Task<PagedResult<VehicleReservation>> Page(
        IEnumerable<VehicleReservation> source,
        int page,
        int pageSize)
    {
        var all = source.ToArray();
        return Task.FromResult(new PagedResult<VehicleReservation>(
            all.Skip((page - 1) * pageSize).Take(pageSize).ToArray(),
            page,
            pageSize,
            all.Length));
    }
}

internal sealed class FakeCatalogOutboxRepository : ICatalogOutboxRepository
{
    public List<CatalogOutbox> Items { get; } = [];

    public Task AddAsync(CatalogOutbox item, CancellationToken cancellationToken)
    {
        Items.Add(item);
        return Task.CompletedTask;
    }

    public Task<CatalogOutbox?> GetByVehicleVersionAsync(
        Guid vehicleId,
        int vehicleVersion,
        CancellationToken cancellationToken) =>
        Task.FromResult(Items.SingleOrDefault(item => item.VehicleId == vehicleId && item.VehicleVersion == vehicleVersion));

    public Task<IReadOnlyList<CatalogOutbox>> GetPendingAsync(
        DateTimeOffset nowUtc,
        int batchSize,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CatalogOutbox>>(Items
            .Where(item => item.ProcessedAtUtc is null && (item.NextAttemptAtUtc is null || item.NextAttemptAtUtc <= nowUtc))
            .Take(batchSize)
            .ToArray());
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCalls { get; private set; }

    public int Commits { get; private set; }

    public Task<ITransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken) =>
        Task.FromResult<ITransaction>(new FakeTransaction(this));

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCalls++;
        return Task.CompletedTask;
    }

    private sealed class FakeTransaction(FakeUnitOfWork owner) : ITransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken)
        {
            owner.Commits++;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

internal sealed class FakeSalesCatalogClient(Result result) : ISalesCatalogClient
{
    public int Calls { get; private set; }

    public Task<Result> UpsertVehicleAsync(Guid vehicleId, string payloadJson, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(result);
    }
}

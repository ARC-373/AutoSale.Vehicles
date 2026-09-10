using AutoSale.Application.Catalog.Rebuild;
using AutoSale.Domain.Catalog;
using AutoSale.Domain.Vehicles;
using AutoSale.Vehicles.Application.UnitTests.Fakes;

namespace AutoSale.Vehicles.Application.UnitTests.Catalog;

public sealed class RebuildCatalogHandlerTests
{
    [Fact]
    public async Task Rebuild_ShouldAddOnlyMissingVehicleVersions()
    {
        var now = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        var first = Vehicle.Create("Honda", "Civic", 2025, "Black", 150_000m, now).Value!;
        var second = Vehicle.Create("Toyota", "Corolla", 2025, "White", 160_000m, now).Value!;
        var vehicles = new FakeVehicleRepository();
        vehicles.Vehicles.Add(first.Id, first);
        vehicles.Vehicles.Add(second.Id, second);
        var outbox = new FakeCatalogOutboxRepository();
        outbox.Items.Add(CatalogOutbox.Create(first.Id, first.Version, "{}", now).Value!);
        var handler = new RebuildCatalogHandler(vehicles, outbox, new FakeUnitOfWork(), new TestClock(now));

        var result = await handler.HandleAsync(new RebuildCatalogCommand(100), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
        Assert.Equal(2, outbox.Items.Count);
        Assert.Contains(outbox.Items, item => item.VehicleId == second.Id && item.VehicleVersion == second.Version);
    }
}

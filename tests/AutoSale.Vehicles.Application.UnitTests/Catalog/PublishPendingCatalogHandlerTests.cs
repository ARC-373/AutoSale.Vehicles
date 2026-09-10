using AutoSale.Application.Catalog.PublishPending;
using AutoSale.Domain.Catalog;
using AutoSale.SharedKernel.Results;
using AutoSale.Vehicles.Application.UnitTests.Fakes;

namespace AutoSale.Vehicles.Application.UnitTests.Catalog;

public sealed class PublishPendingCatalogHandlerTests
{
    [Fact]
    public async Task Handle_ShouldLeasePublishAndCompletePendingItem()
    {
        var now = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        var outbox = new FakeCatalogOutboxRepository();
        outbox.Items.Add(CatalogOutbox.Create(Guid.NewGuid(), 1, "{}", now).Value!);
        var client = new FakeSalesCatalogClient(Result.Success());
        var unitOfWork = new FakeUnitOfWork();
        var handler = new PublishPendingCatalogHandler(outbox, client, unitOfWork, new TestClock(now));

        var result = await handler.HandleAsync(
            new PublishPendingCatalogCommand("worker-1", 20, TimeSpan.FromMinutes(1)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new CatalogPublishSummary(1, 1, 0), result.Value);
        Assert.NotNull(outbox.Items[0].ProcessedAtUtc);
        Assert.Equal(1, outbox.Items[0].Attempts);
        Assert.Equal(1, client.Calls);
        Assert.Equal(1, unitOfWork.Commits);
    }

    [Fact]
    public async Task Handle_ShouldScheduleRetryUsingOnlySanitizedErrorCode()
    {
        var now = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        var outbox = new FakeCatalogOutboxRepository();
        outbox.Items.Add(CatalogOutbox.Create(Guid.NewGuid(), 1, "{}", now).Value!);
        var error = new Error("sales.unavailable", "Sensitive remote details", ErrorType.Failure);
        var handler = new PublishPendingCatalogHandler(
            outbox,
            new FakeSalesCatalogClient(Result.Failure(error)),
            new FakeUnitOfWork(),
            new TestClock(now));

        var result = await handler.HandleAsync(
            new PublishPendingCatalogCommand("worker-1", 20, TimeSpan.FromMinutes(1)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new CatalogPublishSummary(1, 0, 1), result.Value);
        Assert.Equal("sales.unavailable", outbox.Items[0].LastError);
        Assert.DoesNotContain("Sensitive", outbox.Items[0].LastError, StringComparison.Ordinal);
        Assert.Equal(now.AddSeconds(2), outbox.Items[0].NextAttemptAtUtc);
    }
}

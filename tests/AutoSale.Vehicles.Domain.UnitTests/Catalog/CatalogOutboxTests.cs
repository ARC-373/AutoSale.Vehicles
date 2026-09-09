using AutoSale.Domain.Catalog;

namespace AutoSale.Vehicles.Domain.UnitTests.Catalog;

public sealed class CatalogOutboxTests
{
    [Fact]
    public void DeliveryAttempt_ShouldAcquireLeaseAndRecordFailure()
    {
        var now = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        var item = CatalogOutbox.Create(Guid.NewGuid(), 2, "{\"version\":2}", now).Value!;

        var lease = item.AcquireLease("worker-1", now.AddMinutes(1), now);
        item.MarkFailed("temporary failure", now.AddMinutes(5));

        Assert.True(lease.IsSuccess);
        Assert.Equal(1, item.Attempts);
        Assert.Equal(now.AddMinutes(5), item.NextAttemptAtUtc);
        Assert.Equal("temporary failure", item.LastError);
        Assert.Null(item.LeaseOwner);
        Assert.Null(item.LeaseExpiresAtUtc);
    }
}

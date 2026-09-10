namespace AutoSale.Application.Catalog.PublishPending;

public sealed record PublishPendingCatalogCommand(
    string WorkerId,
    int BatchSize,
    TimeSpan LeaseDuration);

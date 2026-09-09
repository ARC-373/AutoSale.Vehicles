namespace AutoSale.Application.Catalog.PublishPending;

public sealed record CatalogPublishSummary(int Claimed, int Published, int Failed);

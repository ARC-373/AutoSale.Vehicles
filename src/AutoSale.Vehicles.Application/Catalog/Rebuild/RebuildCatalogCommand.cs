namespace AutoSale.Application.Catalog.Rebuild;

public sealed record RebuildCatalogCommand(int BatchSize = 100);

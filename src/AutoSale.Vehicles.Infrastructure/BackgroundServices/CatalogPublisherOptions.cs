namespace AutoSale.Infrastructure.BackgroundServices;

public sealed class CatalogPublisherOptions
{
    public const string SectionName = "CatalogPublisher";

    public bool Enabled { get; set; }

    public int BatchSize { get; set; } = 20;

    public int PollIntervalSeconds { get; set; } = 5;

    public int LeaseDurationSeconds { get; set; } = 30;

    public string? WorkerId { get; set; }
}

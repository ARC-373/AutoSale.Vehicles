namespace AutoSale.Infrastructure.Integrations.Sales;

public sealed class SalesIntegrationOptions
{
    public const string SectionName = "Integrations:Sales";
    public const string DefaultServiceKeyHeaderName = "X-Service-Key";

    public string BaseAddress { get; set; } = string.Empty;

    public string ServiceKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 10;

    public bool AllowInsecureHttp { get; set; }
}

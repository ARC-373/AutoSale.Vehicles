using AutoSale.SharedKernel.Results;

namespace AutoSale.Infrastructure.Integrations.Sales;

internal static class SalesCatalogClientErrors
{
    public static readonly Error InvalidConfiguration = new("sales.catalog.configuration_invalid", "Sales catalog integration is not configured.", ErrorType.Failure);
    public static readonly Error Conflict = new("sales.catalog.version_conflict", "Sales rejected a divergent catalog snapshot.", ErrorType.Conflict);
    public static readonly Error EndpointNotFound = new("sales.catalog.endpoint_not_found", "The Sales catalog endpoint was not found.", ErrorType.Failure);
    public static readonly Error Rejected = new("sales.catalog.rejected", "Sales rejected the catalog snapshot.", ErrorType.Failure);
    public static readonly Error Timeout = new("sales.catalog.timeout", "Sales catalog publication timed out.", ErrorType.Failure);
    public static readonly Error Unavailable = new("sales.catalog.unavailable", "Sales catalog is temporarily unavailable.", ErrorType.Failure);
}

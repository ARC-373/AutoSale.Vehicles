using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Abstractions.Integrations;

public interface ISalesCatalogClient
{
    Task<Result> UpsertVehicleAsync(Guid vehicleId, string payloadJson, CancellationToken cancellationToken);
}

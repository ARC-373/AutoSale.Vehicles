using System.Net;
using System.Text;
using AutoSale.Application.Abstractions.Integrations;
using AutoSale.SharedKernel.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoSale.Infrastructure.Integrations.Sales;

public sealed class SalesCatalogClient : ISalesCatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly SalesIntegrationOptions _options;
    private readonly ILogger<SalesCatalogClient> _logger;

    public SalesCatalogClient(
        HttpClient httpClient,
        IOptions<SalesIntegrationOptions> options,
        ILogger<SalesCatalogClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result> UpsertVehicleAsync(
        Guid vehicleId,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        if (!TryBuildEndpoint(vehicleId, out var endpoint) || string.IsNullOrWhiteSpace(_options.ServiceKey))
        {
            return Result.Failure(SalesCatalogClientErrors.InvalidConfiguration);
        }

        using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
        request.Headers.TryAddWithoutValidation(SalesIntegrationOptions.DefaultServiceKeyHeaderName, _options.ServiceKey);
        request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            _logger.LogWarning(
                "Sales catalog rejected vehicle {VehicleId} with HTTP status {StatusCode}",
                vehicleId,
                (int)response.StatusCode);

            return response.StatusCode switch
            {
                HttpStatusCode.Conflict => Result.Failure(SalesCatalogClientErrors.Conflict),
                HttpStatusCode.NotFound => Result.Failure(SalesCatalogClientErrors.EndpointNotFound),
                _ => Result.Failure(SalesCatalogClientErrors.Rejected)
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(SalesCatalogClientErrors.Timeout);
        }
        catch (HttpRequestException)
        {
            return Result.Failure(SalesCatalogClientErrors.Unavailable);
        }
    }

    private bool TryBuildEndpoint(Guid vehicleId, out Uri? endpoint)
    {
        endpoint = null;
        return Uri.TryCreate(_options.BaseAddress, UriKind.Absolute, out var baseAddress) &&
               (baseAddress.Scheme == Uri.UriSchemeHttps ||
                (_options.AllowInsecureHttp && baseAddress.Scheme == Uri.UriSchemeHttp)) &&
               Uri.TryCreate(baseAddress, $"internal/v1/catalog/vehicles/{vehicleId:D}", out endpoint);
    }
}

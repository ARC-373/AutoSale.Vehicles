using System.Net;
using AutoSale.Infrastructure.Integrations.Sales;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AutoSale.Vehicles.Infrastructure.UnitTests.Integrations.Sales;

public sealed class SalesCatalogClientTests
{
    [Fact]
    public async Task Upsert_ShouldSendVersionedPayloadAndServiceCredential()
    {
        var vehicleId = Guid.NewGuid();
        const string payload = "{\"id\":\"vehicle\",\"version\":2}";
        var transport = new RecordingHandler(HttpStatusCode.NoContent);
        var client = CreateClient(transport);

        var result = await client.UpsertVehicleAsync(vehicleId, payload, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Put, transport.Method);
        Assert.Equal($"https://sales.example/internal/v1/catalog/vehicles/{vehicleId:D}", transport.RequestUri!.AbsoluteUri);
        Assert.Equal("integration-secret", transport.ServiceKey);
        Assert.Equal(payload, transport.Payload);
        Assert.Equal("application/json", transport.ContentType);
    }

    [Fact]
    public async Task Upsert_ShouldMapDivergentVersionToStableConflict()
    {
        var client = CreateClient(new RecordingHandler(HttpStatusCode.Conflict));

        var result = await client.UpsertVehicleAsync(Guid.NewGuid(), "{}", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("sales.catalog.version_conflict", result.Error.Code);
    }

    [Fact]
    public async Task Upsert_ShouldFailClosedWhenCredentialIsMissing()
    {
        var transport = new RecordingHandler(HttpStatusCode.NoContent);
        var client = CreateClient(transport, serviceKey: string.Empty);

        var result = await client.UpsertVehicleAsync(Guid.NewGuid(), "{}", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("sales.catalog.configuration_invalid", result.Error.Code);
        Assert.Equal(0, transport.Calls);
    }

    private static SalesCatalogClient CreateClient(RecordingHandler transport, string serviceKey = "integration-secret") =>
        new(
            new HttpClient(transport),
            Options.Create(new SalesIntegrationOptions
            {
                BaseAddress = "https://sales.example/",
                ServiceKey = serviceKey,
                TimeoutSeconds = 10
            }),
            NullLogger<SalesCatalogClient>.Instance);

    private sealed class RecordingHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public HttpMethod? Method { get; private set; }

        public Uri? RequestUri { get; private set; }

        public string? ServiceKey { get; private set; }

        public string? Payload { get; private set; }

        public string? ContentType { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            Method = request.Method;
            RequestUri = request.RequestUri;
            ServiceKey = request.Headers.GetValues(SalesIntegrationOptions.DefaultServiceKeyHeaderName).Single();
            Payload = await request.Content!.ReadAsStringAsync(cancellationToken);
            ContentType = request.Content.Headers.ContentType!.MediaType;
            return new HttpResponseMessage(statusCode);
        }
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Reservations;
using AutoSale.Application.Vehicles;
using AutoSale.Application.Vehicles.Reserve;
using AutoSale.Domain.Reservations;
using AutoSale.Domain.Vehicles;
using AutoSale.SharedKernel.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoSale.Vehicles.Api.IntegrationTests;

public sealed class ApiPipelineTests : IClassFixture<ApiPipelineTests.ApiFactory>
{
    private const string ServiceKey = "integration-test-service-key";
    private readonly HttpClient _client;

    public ApiPipelineTests(ApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Liveness_ShouldNotRequireDatabaseOrAuthentication()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/api/v1/vehicles")]
    [InlineData("/api/v1/reservations")]
    public async Task AdministrativeRoutes_ShouldReturnStructuredUnauthorizedProblem(string route)
    {
        var response = await _client.GetAsync(route);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthenticated", problem.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("/confirmation")]
    [InlineData("/release")]
    public async Task InternalReservationCommands_ShouldRequireServiceKey(string suffix)
    {
        var route = BuildReservationRoute() + suffix;
        var response = await _client.PutAsJsonAsync(route, new { expectedPrice = 150_000m });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MalformedInternalRequest_ShouldReturnStableValidationProblem()
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, BuildReservationRoute())
        {
            Content = new StringContent("{not-json", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Service-Key", ServiceKey);

        var response = await _client.SendAsync(request);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("request.invalid", problem.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task InternalReservation_ShouldUseServiceAuthenticationAndStringEnums()
    {
        var vehicleId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var route = BuildReservationRoute(vehicleId, saleId);
        using var request = new HttpRequestMessage(HttpMethod.Put, route)
        {
            Content = JsonContent.Create(new { expectedPrice = 150_000m })
        };
        request.Headers.Add("X-Service-Key", ServiceKey);

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(saleId, body.GetProperty("saleId").GetGuid());
        Assert.Equal("Reserved", body.GetProperty("reservationStatus").GetString());
        Assert.Equal("Reserved", body.GetProperty("vehicle").GetProperty("status").GetString());
    }

    private static string BuildReservationRoute(Guid? vehicleId = null, Guid? saleId = null) =>
        $"/internal/v1/vehicles/{vehicleId ?? Guid.NewGuid():D}/reservations/{saleId ?? Guid.NewGuid():D}";

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public ApiFactory()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Docker");
            Environment.SetEnvironmentVariable("ConnectionStrings__AutoSale", "Host=localhost;Database=autosale");
            Environment.SetEnvironmentVariable("ServiceAuthentication__ServiceKey", ServiceKey);
            Environment.SetEnvironmentVariable("CatalogPublisher__Enabled", "false");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Docker");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICommandHandler<ReserveVehicleCommand, Result<ReservationResponseDto>>>();
                services.AddSingleton<ICommandHandler<ReserveVehicleCommand, Result<ReservationResponseDto>>, ReservationHandler>();
            });
        }
    }

    private sealed class ReservationHandler : ICommandHandler<ReserveVehicleCommand, Result<ReservationResponseDto>>
    {
        public Task<Result<ReservationResponseDto>> HandleAsync(
            ReserveVehicleCommand command,
            CancellationToken cancellationToken)
        {
            var response = new ReservationResponseDto(
                command.SaleId,
                ReservationStatus.Reserved,
                new VehicleSnapshotDto(
                    command.VehicleId,
                    "Honda",
                    "Civic",
                    2025,
                    "Black",
                    command.ExpectedPrice,
                    VehicleStatus.Reserved,
                    2,
                    DateTimeOffset.UtcNow),
                true);
            return Task.FromResult(Result.Success(response));
        }
    }
}

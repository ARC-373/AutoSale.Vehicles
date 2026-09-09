using System.Text.Encodings.Web;
using AutoSale.Api.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AutoSale.Vehicles.Api.UnitTests.Authentication;

public sealed class ServiceKeyAuthenticationHandlerTests
{
    [Fact]
    public async Task Authenticate_ShouldAcceptConfiguredServiceKey()
    {
        const string serviceKey = "development-test-service-key";
        var options = new StaticOptionsMonitor<ServiceKeyAuthenticationOptions>(new ServiceKeyAuthenticationOptions
        {
            HeaderName = ServiceKeyAuthenticationDefaults.DefaultHeaderName,
            ServiceKey = serviceKey
        });
        var handler = new ServiceKeyAuthenticationHandler(options, NullLoggerFactory.Instance, UrlEncoder.Default);
        var context = new DefaultHttpContext();
        context.Request.Headers[ServiceKeyAuthenticationDefaults.DefaultHeaderName] = serviceKey;
        var scheme = new AuthenticationScheme(
            ServiceKeyAuthenticationDefaults.Scheme,
            ServiceKeyAuthenticationDefaults.Scheme,
            typeof(ServiceKeyAuthenticationHandler));
        await handler.InitializeAsync(scheme, context);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.Equal("sales-service", result.Principal!.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    }

    private sealed class StaticOptionsMonitor<TOptions>(TOptions currentValue) : IOptionsMonitor<TOptions>
    {
        public TOptions CurrentValue => currentValue;

        public TOptions Get(string? name) => currentValue;

        public IDisposable? OnChange(Action<TOptions, string?> listener) => null;
    }
}

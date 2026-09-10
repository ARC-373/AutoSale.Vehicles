using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AutoSale.Api.Authentication;

public sealed class ServiceKeyAuthenticationHandler : AuthenticationHandler<ServiceKeyAuthenticationOptions>
{
    public ServiceKeyAuthenticationHandler(
        IOptionsMonitor<ServiceKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrWhiteSpace(Options.ServiceKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Service authentication is not configured."));
        }

        if (!Request.Headers.TryGetValue(Options.HeaderName, out var values) || values.Count != 1)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(Options.ServiceKey));
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(values[0]!));
        if (!CryptographicOperations.FixedTimeEquals(configuredHash, suppliedHash))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid service credential."));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "sales-service")],
            ServiceKeyAuthenticationDefaults.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ServiceKeyAuthenticationDefaults.Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

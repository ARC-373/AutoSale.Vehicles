using Microsoft.AspNetCore.Authentication;

namespace AutoSale.Api.Authentication;

public sealed class ServiceKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public string HeaderName { get; set; } = ServiceKeyAuthenticationDefaults.DefaultHeaderName;

    public string ServiceKey { get; set; } = string.Empty;
}

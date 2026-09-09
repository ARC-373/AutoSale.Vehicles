using AutoSale.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace AutoSale.Api.Authorization;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAutoSaleAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context => context.User
                    .FindAll(AuthorizationPolicies.CognitoGroupsClaimType)
                    .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Contains(AuthorizationPolicies.AdministratorsGroup, StringComparer.Ordinal));
            })
            .AddPolicy(AuthorizationPolicies.InternalSales, policy =>
            {
                policy.AddAuthenticationSchemes(ServiceKeyAuthenticationDefaults.Scheme);
                policy.RequireAuthenticatedUser();
            });

        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationMiddlewareResultHandler>();

        return services;
    }
}

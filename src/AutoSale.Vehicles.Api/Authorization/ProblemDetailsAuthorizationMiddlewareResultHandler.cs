using AutoSale.Api.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace AutoSale.Api.Authorization;

public sealed class ProblemDetailsAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            if (policy.AuthenticationSchemes.Count == 0)
            {
                await context.ChallengeAsync();
            }
            else
            {
                foreach (var scheme in policy.AuthenticationSchemes)
                {
                    await context.ChallengeAsync(scheme);
                }
            }

            await ApiProblemDetails.WriteAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "auth.unauthenticated",
                "Unauthorized",
                "Authentication is required.");
            return;
        }

        if (authorizeResult.Forbidden)
        {
            await ApiProblemDetails.WriteAsync(
                context,
                StatusCodes.Status403Forbidden,
                "auth.forbidden",
                "Forbidden",
                "The authenticated principal cannot perform this operation.");
            return;
        }

        await next(context);
    }
}

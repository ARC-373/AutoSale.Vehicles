using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AutoSale.Api.Extensions;

public static class ApiProblemDetails
{
    public static ProblemDetails Create(
        HttpContext context,
        int statusCode,
        string code,
        string title,
        string detail)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        return problem;
    }

    public static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        string code,
        string title,
        string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(Create(context, statusCode, code, title, detail));
    }
}

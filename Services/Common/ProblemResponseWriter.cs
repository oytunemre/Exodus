using Microsoft.AspNetCore.Mvc;

namespace Exodus.Services.Common;

public static class ProblemResponseWriter
{
    public static async Task WriteAsync(HttpContext context, int statusCode, string title, string detail)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json");
    }
}

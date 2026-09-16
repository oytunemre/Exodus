using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Exodus.Services.Common;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (statusCode, title, detail) = Translate(ex);

            if (statusCode >= 500)
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning(ex, "Request failed with {StatusCode} for {Method} {Path}", statusCode, context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
                throw;

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

            await context.Response.WriteAsJsonAsync(problem);
        }
    }

    private (int StatusCode, string Title, string Detail) Translate(Exception ex)
    {
        switch (ex)
        {
            case ApiException api:
                return (api.StatusCode, "Request failed", api.Message);

            case UnauthorizedAccessException:
                return (StatusCodes.Status403Forbidden, "Forbidden", ex.Message);

            case ArgumentException:
                return (StatusCodes.Status400BadRequest, "Invalid request", ex.Message);

            default:
                var detail = _env.IsDevelopment()
                    ? $"{ex.Message} | InnerException: {ex.InnerException?.Message} | {ex.InnerException?.InnerException?.Message}"
                    : "An unexpected error occurred.";
                return (StatusCodes.Status500InternalServerError, "Server error", detail);
        }
    }
}

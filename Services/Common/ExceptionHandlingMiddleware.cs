using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Exodus.Services.Common;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";

            var problem = new ProblemDetails
            {
                Status = ex.StatusCode,
                Title = "Request failed",
                Detail = ex.Message
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            // Show full exception chain in development for easier debugging
            var env = context.RequestServices.GetService<IWebHostEnvironment>();
            var detail = env?.IsDevelopment() == true
                ? $"{ex.Message} | InnerException: {ex.InnerException?.Message} | {ex.InnerException?.InnerException?.Message}"
                : ex.Message;

            var problem = new ProblemDetails
            {
                Status = 500,
                Title = "Server error",
                Detail = detail
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}

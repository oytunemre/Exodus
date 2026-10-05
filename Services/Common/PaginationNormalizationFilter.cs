using Microsoft.AspNetCore.Mvc.Filters;

namespace Exodus.Services.Common;

/// <summary>
/// Normalizes <c>page</c>/<c>pageSize</c> action arguments so paged queries never
/// receive a negative offset or a zero page size.
/// </summary>
public sealed class PaginationNormalizationFilter : IActionFilter
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionArguments.TryGetValue("page", out var page) && page is int pageValue && pageValue < 1)
            context.ActionArguments["page"] = 1;

        if (context.ActionArguments.TryGetValue("pageSize", out var pageSize) && pageSize is int pageSizeValue)
        {
            if (pageSizeValue < 1)
                context.ActionArguments["pageSize"] = DefaultPageSize;
            else if (pageSizeValue > MaxPageSize)
                context.ActionArguments["pageSize"] = MaxPageSize;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}

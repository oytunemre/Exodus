using Exodus.Models.Dto;
using Exodus.Models.Entities;

namespace Exodus.Services.Listings;

/// <summary>
/// Filter and sort logic shared by the public listing search and the admin listing list.
/// </summary>
public static class ListingFilters
{
    public static IQueryable<Listing> Apply(this IQueryable<Listing> q, ListingSearchDto f)
    {
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var term = f.Search.Trim().ToLower();
            q = q.Where(l =>
                l.Product.ProductName.ToLower().Contains(term) ||
                l.Product.ProductDescription.ToLower().Contains(term) ||
                (l.Product.Brand != null && l.Product.Brand.ToLower().Contains(term)) ||
                (l.Product.Category != null && l.Product.Category.Name.ToLower().Contains(term)) ||
                (l.SKU != null && l.SKU.ToLower().Contains(term)) ||
                l.Seller.Name.ToLower().Contains(term));
        }

        if (f.CategoryId.HasValue)
            q = q.Where(l => l.Product.CategoryId == f.CategoryId.Value);

        if (f.ProductId.HasValue)
            q = q.Where(l => l.ProductId == f.ProductId.Value);

        if (f.MinPrice.HasValue)
            q = q.Where(l => l.Price >= f.MinPrice.Value);

        if (f.MaxPrice.HasValue)
            q = q.Where(l => l.Price <= f.MaxPrice.Value);

        if (f.Condition.HasValue)
            q = q.Where(l => l.Condition == f.Condition.Value);

        if (f.SellerId.HasValue)
            q = q.Where(l => l.SellerId == f.SellerId.Value);

        if (f.StockStatus.HasValue)
            q = q.Where(l => l.StockStatus == f.StockStatus.Value);

        if (f.InStock == true)
            q = q.Where(l => l.StockQuantity > 0);
        else if (f.InStock == false)
            q = q.Where(l => l.StockQuantity <= 0);

        return q;
    }

    /// <summary>
    /// Sort keys: price, createdAt (default), name (alias: product), stock, seller. Ties break on Id for stable paging.
    /// </summary>
    public static IQueryable<Listing> Sort(this IQueryable<Listing> q, string? sortBy, bool desc)
    {
        var ordered = sortBy?.ToLowerInvariant() switch
        {
            "price" => desc ? q.OrderByDescending(l => l.Price) : q.OrderBy(l => l.Price),
            "stock" => desc ? q.OrderByDescending(l => l.StockQuantity) : q.OrderBy(l => l.StockQuantity),
            "seller" => desc ? q.OrderByDescending(l => l.Seller.Name) : q.OrderBy(l => l.Seller.Name),
            "name" or "product" => desc
                ? q.OrderByDescending(l => l.Product.ProductName)
                : q.OrderBy(l => l.Product.ProductName),
            _ => desc ? q.OrderByDescending(l => l.CreatedAt) : q.OrderBy(l => l.CreatedAt)
        };

        return desc ? ordered.ThenByDescending(l => l.Id) : ordered.ThenBy(l => l.Id);
    }
}

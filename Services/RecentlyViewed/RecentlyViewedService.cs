using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Services.RecentlyViewedProducts;

public class RecentlyViewedService : IRecentlyViewedService
{
    private readonly ApplicationDbContext _db;

    public RecentlyViewedService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task TrackViewAsync(int userId, int productId, CancellationToken ct = default)
    {
        var existing = await _db.Set<RecentlyViewed>()
            .FirstOrDefaultAsync(rv => rv.UserId == userId && rv.ProductId == productId, ct);

        if (existing != null)
        {
            existing.ViewedAt = DateTime.UtcNow;
            existing.ViewCount++;
        }
        else
        {
            _db.Set<RecentlyViewed>().Add(new RecentlyViewed
            {
                UserId = userId,
                ProductId = productId,
                ViewedAt = DateTime.UtcNow,
                ViewCount = 1
            });

            // Maksimum 50 kayit tut, eskileri temizle
            var oldItems = await _db.Set<RecentlyViewed>()
                .Where(rv => rv.UserId == userId)
                .OrderByDescending(rv => rv.ViewedAt)
                .Skip(50)
                .ToListAsync(ct);

            if (oldItems.Any())
                _db.Set<RecentlyViewed>().RemoveRange(oldItems);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<RecentlyViewedDto>> GetRecentlyViewedAsync(int userId, int count = 20, CancellationToken ct = default)
    {
        var items = await _db.Set<RecentlyViewed>()
            .Include(rv => rv.Product)
                .ThenInclude(p => p.Images)
            .Where(rv => rv.UserId == userId)
            .OrderByDescending(rv => rv.ViewedAt)
            .Take(count)
            .ToListAsync(ct);

        var productIds = items.Select(i => i.ProductId).ToList();
        var minPrices = await _db.Listings
            .Where(l => productIds.Contains(l.ProductId) && l.IsActive)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, MinPrice = g.Min(l => l.Price) })
            .ToDictionaryAsync(x => x.ProductId, x => x.MinPrice, ct);

        return items.Select(rv => new RecentlyViewedDto
        {
            ProductId = rv.ProductId,
            ProductName = rv.Product?.ProductName ?? string.Empty,
            ImageUrl = rv.Product?.Images?.FirstOrDefault()?.Url,
            MinPrice = minPrices.GetValueOrDefault(rv.ProductId),
            ViewCount = rv.ViewCount,
            LastViewedAt = rv.ViewedAt
        }).ToList();
    }

    public async Task ClearHistoryAsync(int userId, CancellationToken ct = default)
    {
        var items = await _db.Set<RecentlyViewed>()
            .Where(rv => rv.UserId == userId)
            .ToListAsync(ct);

        _db.Set<RecentlyViewed>().RemoveRange(items);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveFromHistoryAsync(int userId, int productId, CancellationToken ct = default)
    {
        var item = await _db.Set<RecentlyViewed>()
            .FirstOrDefaultAsync(rv => rv.UserId == userId && rv.ProductId == productId, ct);

        if (item != null)
        {
            _db.Set<RecentlyViewed>().Remove(item);
            await _db.SaveChangesAsync(ct);
        }
    }
}

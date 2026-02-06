namespace FarmazonDemo.Services.RecentlyViewedProducts;

public interface IRecentlyViewedService
{
    Task TrackViewAsync(int userId, int productId, CancellationToken ct = default);
    Task<List<RecentlyViewedDto>> GetRecentlyViewedAsync(int userId, int count = 20, CancellationToken ct = default);
    Task ClearHistoryAsync(int userId, CancellationToken ct = default);
    Task RemoveFromHistoryAsync(int userId, int productId, CancellationToken ct = default);
}

public class RecentlyViewedDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal? MinPrice { get; set; }
    public int ViewCount { get; set; }
    public DateTime LastViewedAt { get; set; }
}

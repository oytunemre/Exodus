using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Services.Comparison;

public class ProductComparisonService : IProductComparisonService
{
    private readonly ApplicationDbContext _db;
    private const int MaxProductsPerComparison = 4;

    public ProductComparisonService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ComparisonResponseDto> CreateComparisonAsync(int userId, string? name = null, CancellationToken ct = default)
    {
        var comparison = new ProductComparison
        {
            UserId = userId,
            Name = name ?? $"Karsilastirma {DateTime.UtcNow:dd.MM.yyyy HH:mm}"
        };

        _db.Set<ProductComparison>().Add(comparison);
        await _db.SaveChangesAsync(ct);

        return MapToResponseDto(comparison);
    }

    public async Task<ComparisonResponseDto> AddProductAsync(int userId, int comparisonId, int productId, CancellationToken ct = default)
    {
        var comparison = await _db.Set<ProductComparison>()
            .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(c => c.Id == comparisonId && c.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Karsilastirma listesi bulunamadi");

        if (comparison.Items.Count >= MaxProductsPerComparison)
            throw new InvalidOperationException($"Bir karsilastirma listesinde en fazla {MaxProductsPerComparison} urun olabilir");

        if (comparison.Items.Any(i => i.ProductId == productId))
            throw new InvalidOperationException("Bu urun zaten karsilastirma listesinde");

        var product = await _db.Products.FindAsync(new object[] { productId }, ct)
            ?? throw new KeyNotFoundException("Urun bulunamadi");

        var maxOrder = comparison.Items.Any() ? comparison.Items.Max(i => i.DisplayOrder) : 0;

        var item = new ProductComparisonItem
        {
            ComparisonId = comparisonId,
            ProductId = productId,
            DisplayOrder = maxOrder + 1
        };

        _db.Set<ProductComparisonItem>().Add(item);
        await _db.SaveChangesAsync(ct);

        return await GetComparisonAsync(userId, comparisonId, ct);
    }

    public async Task<ComparisonResponseDto> RemoveProductAsync(int userId, int comparisonId, int productId, CancellationToken ct = default)
    {
        var comparison = await _db.Set<ProductComparison>()
            .FirstOrDefaultAsync(c => c.Id == comparisonId && c.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Karsilastirma listesi bulunamadi");

        var item = await _db.Set<ProductComparisonItem>()
            .FirstOrDefaultAsync(i => i.ComparisonId == comparisonId && i.ProductId == productId, ct)
            ?? throw new KeyNotFoundException("Urun karsilastirma listesinde bulunamadi");

        _db.Set<ProductComparisonItem>().Remove(item);
        await _db.SaveChangesAsync(ct);

        return await GetComparisonAsync(userId, comparisonId, ct);
    }

    public async Task<ComparisonResponseDto> GetComparisonAsync(int userId, int comparisonId, CancellationToken ct = default)
    {
        var comparison = await _db.Set<ProductComparison>()
            .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(c => c.Id == comparisonId && c.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Karsilastirma listesi bulunamadi");

        return MapToResponseDto(comparison);
    }

    public async Task<List<ComparisonListDto>> GetUserComparisonsAsync(int userId, CancellationToken ct = default)
    {
        var comparisons = await _db.Set<ProductComparison>()
            .Include(c => c.Items)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);

        return comparisons.Select(c => new ComparisonListDto
        {
            Id = c.Id,
            Name = c.Name,
            ProductCount = c.Items.Count,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task DeleteComparisonAsync(int userId, int comparisonId, CancellationToken ct = default)
    {
        var comparison = await _db.Set<ProductComparison>()
            .FirstOrDefaultAsync(c => c.Id == comparisonId && c.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Karsilastirma listesi bulunamadi");

        _db.Set<ProductComparison>().Remove(comparison);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ComparisonDetailDto> GetDetailedComparisonAsync(int userId, int comparisonId, CancellationToken ct = default)
    {
        var comparison = await _db.Set<ProductComparison>()
            .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
            .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(c => c.Id == comparisonId && c.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Karsilastirma listesi bulunamadi");

        var productIds = comparison.Items.Select(i => i.ProductId).ToList();

        // Fiyat bilgilerini al
        var priceData = await _db.Listings
            .Where(l => productIds.Contains(l.ProductId) && l.IsActive)
            .GroupBy(l => l.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                MinPrice = g.Min(l => l.Price),
                MaxPrice = g.Max(l => l.Price),
                ListingCount = g.Count()
            })
            .ToDictionaryAsync(x => x.ProductId, ct);

        // Attribute bilgilerini al
        var attributeMappings = await _db.ProductAttributeMappings
            .Include(m => m.Attribute)
            .Include(m => m.AttributeValue)
            .Where(m => productIds.Contains(m.ProductId))
            .ToListAsync(ct);

        var allAttributeNames = attributeMappings
            .Select(m => m.Attribute.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        // Review bilgilerini al (ProductId nullable)
        var reviewData = await _db.Reviews
            .Where(r => r.ProductId.HasValue && productIds.Contains(r.ProductId.Value) && r.Status == ReviewStatus.Approved)
            .GroupBy(r => r.ProductId!.Value)
            .Select(g => new
            {
                ProductId = g.Key,
                AverageRating = g.Average(r => r.Rating),
                ReviewCount = g.Count()
            })
            .ToDictionaryAsync(x => x.ProductId, ct);

        var products = comparison.Items
            .OrderBy(i => i.DisplayOrder)
            .Select(item =>
            {
                var product = item.Product!;
                var price = priceData.GetValueOrDefault(product.Id);
                var review = reviewData.GetValueOrDefault(product.Id);
                var attrs = attributeMappings
                    .Where(m => m.ProductId == product.Id)
                    .ToDictionary(m => m.Attribute.Name, m => m.AttributeValue?.Value);

                return new ComparisonProductDetailDto
                {
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    Description = product.ProductDescription,
                    ImageUrl = product.Images?.FirstOrDefault()?.Url,
                    Brand = product.Brand,
                    Category = product.Category?.Name,
                    MinPrice = price?.MinPrice,
                    MaxPrice = price?.MaxPrice,
                    ListingCount = price?.ListingCount ?? 0,
                    AverageRating = review?.AverageRating,
                    ReviewCount = review?.ReviewCount ?? 0,
                    Attributes = allAttributeNames.ToDictionary(
                        name => name,
                        name => attrs.GetValueOrDefault(name))
                };
            }).ToList();

        return new ComparisonDetailDto
        {
            Id = comparison.Id,
            Name = comparison.Name,
            Products = products,
            AttributeNames = allAttributeNames
        };
    }

    private static ComparisonResponseDto MapToResponseDto(ProductComparison comparison)
    {
        return new ComparisonResponseDto
        {
            Id = comparison.Id,
            Name = comparison.Name,
            Products = comparison.Items?.OrderBy(i => i.DisplayOrder).Select(i => new ComparisonProductDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.ProductName ?? string.Empty,
                ImageUrl = i.Product?.Images?.FirstOrDefault()?.Url,
                DisplayOrder = i.DisplayOrder
            }).ToList() ?? new(),
            CreatedAt = comparison.CreatedAt
        };
    }
}

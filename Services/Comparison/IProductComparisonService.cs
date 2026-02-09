namespace Exodus.Services.Comparison;

public interface IProductComparisonService
{
    Task<ComparisonResponseDto> CreateComparisonAsync(int userId, string? name = null, CancellationToken ct = default);
    Task<ComparisonResponseDto> AddProductAsync(int userId, int comparisonId, int productId, CancellationToken ct = default);
    Task<ComparisonResponseDto> RemoveProductAsync(int userId, int comparisonId, int productId, CancellationToken ct = default);
    Task<ComparisonResponseDto> GetComparisonAsync(int userId, int comparisonId, CancellationToken ct = default);
    Task<List<ComparisonListDto>> GetUserComparisonsAsync(int userId, CancellationToken ct = default);
    Task DeleteComparisonAsync(int userId, int comparisonId, CancellationToken ct = default);
    Task<ComparisonDetailDto> GetDetailedComparisonAsync(int userId, int comparisonId, CancellationToken ct = default);
}

public class ComparisonResponseDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public List<ComparisonProductDto> Products { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class ComparisonListDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int ProductCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ComparisonProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public int DisplayOrder { get; set; }
}

public class ComparisonDetailDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public List<ComparisonProductDetailDto> Products { get; set; } = new();
    public List<string> AttributeNames { get; set; } = new();
}

public class ComparisonProductDetailDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int ListingCount { get; set; }
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public Dictionary<string, string?> Attributes { get; set; } = new();
}

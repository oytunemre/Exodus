using Exodus.Models.Enums;

namespace Exodus.Models.Dto
{
    public class ProductSearchDto
    {
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public ListingCondition? Condition { get; set; }
        public int? SellerId { get; set; }
        public bool? InStock { get; set; }
        public string? Brand { get; set; }
        public string SortBy { get; set; } = "createdAt";
        public string SortOrder { get; set; } = "desc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ProductSearchResultDto
    {
        public List<ProductListItemDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class ProductListItemDto
    {
        public int Id { get; set; }
        public required string ProductName { get; set; }
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public decimal? LowestPrice { get; set; }
        public int ListingCount { get; set; }
        public int TotalStock { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ListingSearchDto
    {
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public int? ProductId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public ListingCondition? Condition { get; set; }
        public int? SellerId { get; set; }
        public bool? InStock { get; set; }
        public StockStatus? StockStatus { get; set; }
        public string SortBy { get; set; } = "price";
        public string SortOrder { get; set; } = "asc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ListingSearchResultDto
    {
        public List<ListingListItemDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class ListingListItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public required string ProductName { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public StockStatus StockStatus { get; set; }
        public ListingCondition Condition { get; set; }
        public int SellerId { get; set; }
        public required string SellerName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

using Exodus.Models.Enums;

namespace Exodus.Models.Dto.SellerDto
{
    public class SellerListingItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? PrimaryImage { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public StockStatus StockStatus { get; set; }
        public int LowStockThreshold { get; set; }
        public string? SKU { get; set; }
        public ListingCondition Condition { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

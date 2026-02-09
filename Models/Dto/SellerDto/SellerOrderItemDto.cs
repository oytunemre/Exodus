namespace Exodus.Models.Dto.SellerDto
{
    public class SellerOrderItemDto
    {
        public int SellerOrderItemId { get; set; }
        public int ListingId { get; set; }

        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
    }
}

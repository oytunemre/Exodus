using Exodus.Models.Enums;

namespace Exodus.Models.Dto.ListingDto
{
    public class ListingResponseDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int SellerId { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public ListingCondition Condition { get; set; }
        public bool IsActive { get; set; }
    }
}

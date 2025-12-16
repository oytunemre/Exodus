using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Dto.ListingDto
{
    public class AddListingDto
    {
        public int ProductId { get; set; }
        public int SellerId { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }

        public ListingCondition Condition { get; set; } = ListingCondition.New;
    }
}

using Exodus.Models.Enums;

namespace Exodus.Models.Dto.ListingDto
{
    public class UpdateListingDto
    {
        public decimal Price { get; set; }
        public int Stock { get; set; }

        public ListingCondition? Condition { get; set; }
        public bool IsActive { get; set; }
    }
}

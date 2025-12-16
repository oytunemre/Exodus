using System.ComponentModel.DataAnnotations.Schema;
using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Entities
{
    public class Listing : BaseEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int SellerId { get; set; }
        public Users Seller { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int Stock { get; set; }

        public ListingCondition Condition { get; set; } = ListingCondition.New;

        public bool IsActive { get; set; } = true;
    }
}

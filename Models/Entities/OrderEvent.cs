using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Exodus.Models.Enums;

namespace Exodus.Models.Entities
{
    public class OrderEvent : BaseEntity
    {
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order Order { get; set; } = null!;

        public OrderStatus Status { get; set; }

        [Required]
        [StringLength(200)]
        public required string Title { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        // Who triggered this event
        public int? UserId { get; set; }
        [StringLength(50)]
        public string? UserType { get; set; } // Customer, Seller, Admin, System

        // Additional data (JSON)
        [StringLength(2000)]
        public string? Metadata { get; set; }
    }
}

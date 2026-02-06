using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Exodus.Models.Entities
{
    public class Notification : BaseEntity
    {
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public required string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public required string Message { get; set; }

        public NotificationType Type { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        [StringLength(500)]
        public string? ActionUrl { get; set; }

        [StringLength(100)]
        public string? Icon { get; set; }

        // Related entity info (optional)
        [StringLength(50)]
        public string? RelatedEntityType { get; set; } // Order, Product, etc.

        public int? RelatedEntityId { get; set; }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        OrderUpdate,
        PaymentUpdate,
        ShipmentUpdate,
        PriceAlert,
        StockAlert,
        Promotion,
        System
    }
}

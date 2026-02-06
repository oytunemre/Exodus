using System.ComponentModel.DataAnnotations.Schema;

namespace Exodus.Models.Entities
{
    public class NotificationPreferences : BaseEntity
    {
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; } = null!;

        // Email Notifications
        public bool EmailOrderUpdates { get; set; } = true;
        public bool EmailPaymentUpdates { get; set; } = true;
        public bool EmailShipmentUpdates { get; set; } = true;
        public bool EmailPromotions { get; set; } = true;
        public bool EmailNewsletter { get; set; } = false;
        public bool EmailPriceAlerts { get; set; } = true;
        public bool EmailStockAlerts { get; set; } = true;

        // Push Notifications (for future mobile app)
        public bool PushOrderUpdates { get; set; } = true;
        public bool PushPaymentUpdates { get; set; } = true;
        public bool PushShipmentUpdates { get; set; } = true;
        public bool PushPromotions { get; set; } = false;
        public bool PushPriceAlerts { get; set; } = true;
        public bool PushStockAlerts { get; set; } = true;

        // SMS Notifications
        public bool SmsOrderUpdates { get; set; } = false;
        public bool SmsShipmentUpdates { get; set; } = true;
        public bool SmsPromotions { get; set; } = false;
    }
}

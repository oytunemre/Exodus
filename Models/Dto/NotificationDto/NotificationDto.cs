using FarmazonDemo.Models.Entities;

namespace FarmazonDemo.Models.Dto
{
    public class NotificationResponseDto
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Message { get; set; }
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? ActionUrl { get; set; }
        public string? Icon { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationListDto
    {
        public List<NotificationResponseDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class CreateNotificationDto
    {
        public int UserId { get; set; }
        public required string Title { get; set; }
        public required string Message { get; set; }
        public NotificationType Type { get; set; } = NotificationType.Info;
        public string? ActionUrl { get; set; }
        public string? Icon { get; set; }
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
    }

    public class NotificationPreferencesDto
    {
        // Email
        public bool EmailOrderUpdates { get; set; }
        public bool EmailPaymentUpdates { get; set; }
        public bool EmailShipmentUpdates { get; set; }
        public bool EmailPromotions { get; set; }
        public bool EmailNewsletter { get; set; }
        public bool EmailPriceAlerts { get; set; }
        public bool EmailStockAlerts { get; set; }

        // Push
        public bool PushOrderUpdates { get; set; }
        public bool PushPaymentUpdates { get; set; }
        public bool PushShipmentUpdates { get; set; }
        public bool PushPromotions { get; set; }
        public bool PushPriceAlerts { get; set; }
        public bool PushStockAlerts { get; set; }

        // SMS
        public bool SmsOrderUpdates { get; set; }
        public bool SmsShipmentUpdates { get; set; }
        public bool SmsPromotions { get; set; }
    }

    public class UpdateNotificationPreferencesDto
    {
        // Email
        public bool? EmailOrderUpdates { get; set; }
        public bool? EmailPaymentUpdates { get; set; }
        public bool? EmailShipmentUpdates { get; set; }
        public bool? EmailPromotions { get; set; }
        public bool? EmailNewsletter { get; set; }
        public bool? EmailPriceAlerts { get; set; }
        public bool? EmailStockAlerts { get; set; }

        // Push
        public bool? PushOrderUpdates { get; set; }
        public bool? PushPaymentUpdates { get; set; }
        public bool? PushShipmentUpdates { get; set; }
        public bool? PushPromotions { get; set; }
        public bool? PushPriceAlerts { get; set; }
        public bool? PushStockAlerts { get; set; }

        // SMS
        public bool? SmsOrderUpdates { get; set; }
        public bool? SmsShipmentUpdates { get; set; }
        public bool? SmsPromotions { get; set; }
    }
}

using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Dto.SellerDto
{
    public class SellerShipmentDto
    {
        public string? Carrier { get; set; }
        public string? TrackingNumber { get; set; }
        public ShipmentStatus Status { get; set; }

        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }
}

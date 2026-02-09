using Exodus.Models.Enums;

namespace Exodus.Models.Dto.SellerDto
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

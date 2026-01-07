using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Entities;

public class Shipment : BaseEntity
{
    public int SellerOrderId { get; set; }
    public SellerOrder SellerOrder { get; set; } = null!;

    // 🔴 BU İSİM ŞART
    public string Carrier { get; set; } = "MANUAL";

    public string? TrackingNumber { get; set; }

    public ShipmentStatus Status { get; set; } = ShipmentStatus.Created;

    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

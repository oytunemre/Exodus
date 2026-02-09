using Exodus.Models.Enums;

namespace Exodus.Models.Dto.Shipment;

public class ShipmentDto
{
    public int Id { get; set; }
    public int SellerOrderId { get; set; }

    public string Carrier { get; set; } = "MANUAL";
    public string? TrackingNumber { get; set; }

    public ShipmentStatus Status { get; set; }

    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

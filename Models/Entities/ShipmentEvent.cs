using Exodus.Models.Enums;

namespace Exodus.Models.Entities;

public class ShipmentEvent
{
    public int ShipmentEventId { get; set; }

    public int ShipmentId { get; set; }
    public Shipment Shipment { get; set; } = null!;
        
    public ShipmentStatus Status { get; set; }

    // Kim yaptı? Ne oldu? (JSON string)
    public string? PayloadJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

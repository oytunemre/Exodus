using Exodus.Models.Enums;

namespace Exodus.Models.Dto.Shipment;

public class ShipmentEventDto
{
    public int Id { get; set; }
    public int? ShipmentId { get; set; }
    public ShipmentStatus Status { get; set; }
    public string? PayloadJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

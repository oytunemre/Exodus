using Exodus.Models.Dto.Shipment;

namespace Exodus.Services.Shipments;

public interface IShipmentService
{
    Task<ShipmentDto> GetBySellerOrderIdAsync(int sellerOrderId, int callerId, bool isAdmin, CancellationToken ct = default);
    Task<ShipmentDto> ShipAsync(int sellerOrderId, ShipSellerOrderDto dto, int callerId, bool isAdmin, CancellationToken ct = default);
    Task<ShipmentDto> DeliverAsync(int sellerOrderId, int callerId, bool isAdmin, CancellationToken ct = default);
    Task<List<ShipmentEventDto>> GetTimelineBySellerOrderIdAsync(int sellerOrderId, int callerId, bool isAdmin, CancellationToken ct = default);
}

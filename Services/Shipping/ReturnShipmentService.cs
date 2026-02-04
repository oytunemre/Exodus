using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Services.Shipping;

public interface IReturnShipmentService
{
    Task<ReturnShipment> CreateReturnShipmentAsync(CreateReturnShipmentRequest request);
    Task<ReturnShipment> GetByIdAsync(int id);
    Task<ReturnShipment> GetByCodeAsync(string returnCode);
    Task<ReturnShipment> UpdateStatusAsync(int id, ReturnShipmentStatus status, string? notes = null);
    Task<ReturnShipment> SetTrackingAsync(int id, string trackingNumber, int? carrierId = null);
    Task<decimal> CalculateReturnShippingCostAsync(int sellerOrderId, ReturnReason reason);
    Task<string> GenerateReturnCodeAsync();
}

public class ReturnShipmentService : IReturnShipmentService
{
    private readonly ApplicationDbContext _db;

    public ReturnShipmentService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ReturnShipment> CreateReturnShipmentAsync(CreateReturnShipmentRequest request)
    {
        // Validate seller order exists
        var sellerOrder = await _db.SellerOrders
            .Include(so => so.Seller)
            .FirstOrDefaultAsync(so => so.Id == request.SellerOrderId);

        if (sellerOrder == null)
            throw new NotFoundException("Seller order not found");

        // Determine who pays for shipping based on reason
        var paidBy = request.PaidByOverride ?? request.Reason.GetPaidBy();

        // Calculate shipping cost
        var shippingCost = await CalculateReturnShippingCostAsync(request.SellerOrderId, request.Reason);

        // If seller/platform pays, cost is 0 for the buyer
        if (paidBy != ShippingPaidBy.Buyer)
            shippingCost = 0; // Buyer doesn't pay

        var returnCode = await GenerateReturnCodeAsync();

        var returnShipment = new ReturnShipment
        {
            ReturnCode = returnCode,
            TicketId = request.TicketId,
            RefundId = request.RefundId,
            SellerOrderId = request.SellerOrderId,
            CarrierId = request.CarrierId,
            CarrierName = request.CarrierName,
            Reason = request.Reason,
            ReasonDescription = request.ReasonDescription,
            ShippingCost = shippingCost,
            PaidBy = paidBy,
            Status = ReturnShipmentStatus.CodeGenerated,
            CodeGeneratedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(14), // 14 gün geçerli
            IsPickupRequested = request.IsPickupRequested,
            PickupAddress = request.PickupAddress
        };

        _db.ReturnShipments.Add(returnShipment);
        await _db.SaveChangesAsync();

        return returnShipment;
    }

    public async Task<ReturnShipment> GetByIdAsync(int id)
    {
        var returnShipment = await _db.ReturnShipments
            .Include(r => r.Ticket)
            .Include(r => r.Refund)
            .Include(r => r.SellerOrder)
                .ThenInclude(so => so.Seller)
            .Include(r => r.SellerOrder)
                .ThenInclude(so => so.Order)
            .Include(r => r.Carrier)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (returnShipment == null)
            throw new NotFoundException("Return shipment not found");

        return returnShipment;
    }

    public async Task<ReturnShipment> GetByCodeAsync(string returnCode)
    {
        var returnShipment = await _db.ReturnShipments
            .Include(r => r.Ticket)
            .Include(r => r.Refund)
            .Include(r => r.SellerOrder)
                .ThenInclude(so => so.Seller)
            .Include(r => r.SellerOrder)
                .ThenInclude(so => so.Order)
            .Include(r => r.Carrier)
            .FirstOrDefaultAsync(r => r.ReturnCode == returnCode);

        if (returnShipment == null)
            throw new NotFoundException("Return shipment not found");

        return returnShipment;
    }

    public async Task<ReturnShipment> UpdateStatusAsync(int id, ReturnShipmentStatus status, string? notes = null)
    {
        var returnShipment = await _db.ReturnShipments.FindAsync(id);
        if (returnShipment == null)
            throw new NotFoundException("Return shipment not found");

        var oldStatus = returnShipment.Status;
        returnShipment.Status = status;

        // Update timestamps based on status
        switch (status)
        {
            case ReturnShipmentStatus.InTransit:
                returnShipment.ShippedAt = DateTime.UtcNow;
                break;
            case ReturnShipmentStatus.Delivered:
                returnShipment.ReceivedAt = DateTime.UtcNow;
                break;
        }

        if (!string.IsNullOrEmpty(notes))
            returnShipment.AdminNotes = notes;

        await _db.SaveChangesAsync();

        return returnShipment;
    }

    public async Task<ReturnShipment> SetTrackingAsync(int id, string trackingNumber, int? carrierId = null)
    {
        var returnShipment = await _db.ReturnShipments.FindAsync(id);
        if (returnShipment == null)
            throw new NotFoundException("Return shipment not found");

        returnShipment.TrackingNumber = trackingNumber;

        if (carrierId.HasValue)
        {
            var carrier = await _db.ShippingCarriers.FindAsync(carrierId.Value);
            if (carrier != null)
            {
                returnShipment.CarrierId = carrierId;
                returnShipment.CarrierName = carrier.Name;
            }
        }

        if (returnShipment.Status == ReturnShipmentStatus.CodeGenerated ||
            returnShipment.Status == ReturnShipmentStatus.Pending)
        {
            returnShipment.Status = ReturnShipmentStatus.InTransit;
            returnShipment.ShippedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return returnShipment;
    }

    public async Task<decimal> CalculateReturnShippingCostAsync(int sellerOrderId, ReturnReason reason)
    {
        var sellerOrder = await _db.SellerOrders
            .Include(so => so.Seller)
            .FirstOrDefaultAsync(so => so.Id == sellerOrderId);

        if (sellerOrder == null)
            return 0;

        // Check seller shipping settings
        var sellerSettings = await _db.SellerShippingSettings
            .FirstOrDefaultAsync(s => s.SellerId == sellerOrder.SellerId);

        // If seller offers free returns, cost is 0
        if (sellerSettings?.OffersFreeReturns == true)
            return 0;

        // Get default shipping cost from site settings
        var defaultCostSetting = await _db.SiteSettings
            .FirstOrDefaultAsync(s => s.Key == "Shipping.DefaultCost");

        var defaultCost = decimal.TryParse(defaultCostSetting?.Value, out var cost) ? cost : 29.90m;

        // Use seller's default cost if available
        if (sellerSettings?.DefaultShippingCost > 0)
            defaultCost = sellerSettings.DefaultShippingCost;

        return defaultCost;
    }

    public async Task<string> GenerateReturnCodeAsync()
    {
        var today = DateTime.UtcNow;
        var prefix = $"RET-{today:yyyyMMdd}";

        var lastReturn = await _db.ReturnShipments
            .Where(r => r.ReturnCode.StartsWith(prefix))
            .OrderByDescending(r => r.ReturnCode)
            .FirstOrDefaultAsync();

        int sequence = 1;
        if (lastReturn != null)
        {
            var lastSequence = lastReturn.ReturnCode.Split('-').Last();
            if (int.TryParse(lastSequence, out var num))
                sequence = num + 1;
        }

        return $"{prefix}-{sequence:D4}";
    }
}

public class CreateReturnShipmentRequest
{
    public int? TicketId { get; set; }
    public int? RefundId { get; set; }
    public required int SellerOrderId { get; set; }
    public int? CarrierId { get; set; }
    public string? CarrierName { get; set; }
    public required ReturnReason Reason { get; set; }
    public string? ReasonDescription { get; set; }
    public ShippingPaidBy? PaidByOverride { get; set; } // Admin override
    public bool IsPickupRequested { get; set; } = false;
    public string? PickupAddress { get; set; }
}

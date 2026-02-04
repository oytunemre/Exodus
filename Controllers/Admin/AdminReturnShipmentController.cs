using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.Common;
using FarmazonDemo.Services.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FarmazonDemo.Controllers.Admin;

[Route("api/admin/return-shipments")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminReturnShipmentController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IReturnShipmentService _returnShipmentService;

    public AdminReturnShipmentController(
        ApplicationDbContext db,
        IReturnShipmentService returnShipmentService)
    {
        _db = db;
        _returnShipmentService = returnShipmentService;
    }

    /// <summary>
    /// Get all return shipments with filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetReturnShipments(
        [FromQuery] ReturnShipmentStatus? status = null,
        [FromQuery] ReturnReason? reason = null,
        [FromQuery] ShippingPaidBy? paidBy = null,
        [FromQuery] int? sellerId = null,
        [FromQuery] int? ticketId = null,
        [FromQuery] string? returnCode = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] bool sortDesc = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.ReturnShipments
            .Include(r => r.SellerOrder)
                .ThenInclude(so => so.Seller)
            .Include(r => r.SellerOrder)
                .ThenInclude(so => so.Order)
            .Include(r => r.Carrier)
            .AsQueryable();

        // Filters
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (reason.HasValue)
            query = query.Where(r => r.Reason == reason.Value);

        if (paidBy.HasValue)
            query = query.Where(r => r.PaidBy == paidBy.Value);

        if (sellerId.HasValue)
            query = query.Where(r => r.SellerOrder.SellerId == sellerId.Value);

        if (ticketId.HasValue)
            query = query.Where(r => r.TicketId == ticketId.Value);

        if (!string.IsNullOrEmpty(returnCode))
            query = query.Where(r => r.ReturnCode.Contains(returnCode));

        if (fromDate.HasValue)
            query = query.Where(r => r.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.CreatedAt <= toDate.Value);

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "status" => sortDesc ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
            "reason" => sortDesc ? query.OrderByDescending(r => r.Reason) : query.OrderBy(r => r.Reason),
            _ => sortDesc ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var returns = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.ReturnCode,
                r.Reason,
                r.PaidBy,
                r.Status,
                r.ShippingCost,
                r.TrackingNumber,
                r.CarrierName,
                r.TicketId,
                r.RefundId,
                SellerOrder = new
                {
                    r.SellerOrder.Id,
                    r.SellerOrder.OrderId,
                    OrderNumber = r.SellerOrder.Order.OrderNumber,
                    r.SellerOrder.SubTotal
                },
                Seller = new
                {
                    r.SellerOrder.Seller.Id,
                    r.SellerOrder.Seller.Name
                },
                r.CodeGeneratedAt,
                r.ShippedAt,
                r.ReceivedAt,
                r.ExpiresAt,
                IsExpired = r.ExpiresAt.HasValue && r.ExpiresAt < DateTime.UtcNow && r.Status == ReturnShipmentStatus.CodeGenerated,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Items = returns,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    /// <summary>
    /// Get return shipment details
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetReturnShipment(int id)
    {
        var returnShipment = await _returnShipmentService.GetByIdAsync(id);

        return Ok(new
        {
            returnShipment.Id,
            returnShipment.ReturnCode,
            returnShipment.Reason,
            ReasonDescription = returnShipment.ReasonDescription,
            returnShipment.PaidBy,
            PaidByDescription = returnShipment.PaidBy switch
            {
                ShippingPaidBy.Buyer => "Alıcı tarafından ödeniyor",
                ShippingPaidBy.Seller => "Satıcı tarafından ödeniyor",
                ShippingPaidBy.Platform => "Platform tarafından karşılanıyor",
                _ => "Bilinmiyor"
            },
            returnShipment.Status,
            returnShipment.ShippingCost,
            returnShipment.TrackingNumber,
            returnShipment.CarrierId,
            returnShipment.CarrierName,
            Carrier = returnShipment.Carrier != null ? new
            {
                returnShipment.Carrier.Id,
                returnShipment.Carrier.Name,
                returnShipment.Carrier.TrackingUrlTemplate
            } : null,
            TrackingUrl = returnShipment.Carrier?.TrackingUrlTemplate != null && returnShipment.TrackingNumber != null
                ? returnShipment.Carrier.TrackingUrlTemplate.Replace("{tracking}", returnShipment.TrackingNumber)
                : null,
            returnShipment.TicketId,
            returnShipment.RefundId,
            SellerOrder = new
            {
                returnShipment.SellerOrder.Id,
                returnShipment.SellerOrder.OrderId,
                OrderNumber = returnShipment.SellerOrder.Order.OrderNumber,
                returnShipment.SellerOrder.SubTotal,
                returnShipment.SellerOrder.Status
            },
            Seller = new
            {
                returnShipment.SellerOrder.Seller.Id,
                returnShipment.SellerOrder.Seller.Name,
                returnShipment.SellerOrder.Seller.Email
            },
            returnShipment.IsPickupRequested,
            returnShipment.PickupAddress,
            returnShipment.AdminNotes,
            returnShipment.CodeGeneratedAt,
            returnShipment.ShippedAt,
            returnShipment.ReceivedAt,
            returnShipment.ExpiresAt,
            IsExpired = returnShipment.ExpiresAt.HasValue && returnShipment.ExpiresAt < DateTime.UtcNow &&
                        returnShipment.Status == ReturnShipmentStatus.CodeGenerated,
            returnShipment.CreatedAt,
            returnShipment.UpdatedAt
        });
    }

    /// <summary>
    /// Get return shipment by code
    /// </summary>
    [HttpGet("by-code/{code}")]
    public async Task<ActionResult> GetByCode(string code)
    {
        var returnShipment = await _returnShipmentService.GetByCodeAsync(code);
        return await GetReturnShipment(returnShipment.Id);
    }

    /// <summary>
    /// Create return shipment for a ticket
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreateReturnShipment([FromBody] CreateReturnShipmentDto dto)
    {
        var request = new CreateReturnShipmentRequest
        {
            TicketId = dto.TicketId,
            RefundId = dto.RefundId,
            SellerOrderId = dto.SellerOrderId,
            CarrierId = dto.CarrierId,
            CarrierName = dto.CarrierName,
            Reason = dto.Reason,
            ReasonDescription = dto.ReasonDescription,
            PaidByOverride = dto.PaidByOverride,
            IsPickupRequested = dto.IsPickupRequested,
            PickupAddress = dto.PickupAddress
        };

        var returnShipment = await _returnShipmentService.CreateReturnShipmentAsync(request);

        return CreatedAtAction(nameof(GetReturnShipment), new { id = returnShipment.Id }, new
        {
            Message = "Return shipment created successfully",
            ReturnShipmentId = returnShipment.Id,
            ReturnCode = returnShipment.ReturnCode,
            PaidBy = returnShipment.PaidBy,
            ShippingCost = returnShipment.ShippingCost,
            ExpiresAt = returnShipment.ExpiresAt
        });
    }

    /// <summary>
    /// Update return shipment status
    /// </summary>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateReturnStatusDto dto)
    {
        var returnShipment = await _returnShipmentService.UpdateStatusAsync(id, dto.Status, dto.Notes);

        return Ok(new
        {
            Message = "Return shipment status updated",
            ReturnShipmentId = id,
            Status = returnShipment.Status
        });
    }

    /// <summary>
    /// Set tracking number
    /// </summary>
    [HttpPatch("{id:int}/tracking")]
    public async Task<ActionResult> SetTracking(int id, [FromBody] SetReturnTrackingDto dto)
    {
        var returnShipment = await _returnShipmentService.SetTrackingAsync(id, dto.TrackingNumber, dto.CarrierId);

        return Ok(new
        {
            Message = "Tracking information updated",
            ReturnShipmentId = id,
            TrackingNumber = returnShipment.TrackingNumber,
            CarrierName = returnShipment.CarrierName
        });
    }

    /// <summary>
    /// Update who pays for shipping (admin override)
    /// </summary>
    [HttpPatch("{id:int}/paid-by")]
    public async Task<ActionResult> UpdatePaidBy(int id, [FromBody] UpdatePaidByDto dto)
    {
        var returnShipment = await _db.ReturnShipments.FindAsync(id);
        if (returnShipment == null)
            throw new NotFoundException("Return shipment not found");

        returnShipment.PaidBy = dto.PaidBy;

        // Recalculate cost based on new payer
        if (dto.PaidBy != ShippingPaidBy.Buyer)
        {
            returnShipment.ShippingCost = 0; // Buyer doesn't pay
        }
        else
        {
            // Recalculate
            returnShipment.ShippingCost = await _returnShipmentService
                .CalculateReturnShippingCostAsync(returnShipment.SellerOrderId, returnShipment.Reason);
        }

        if (!string.IsNullOrEmpty(dto.Notes))
            returnShipment.AdminNotes = dto.Notes;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = "Shipping payer updated",
            ReturnShipmentId = id,
            PaidBy = returnShipment.PaidBy,
            ShippingCost = returnShipment.ShippingCost
        });
    }

    /// <summary>
    /// Extend expiration date
    /// </summary>
    [HttpPatch("{id:int}/extend")]
    public async Task<ActionResult> ExtendExpiration(int id, [FromBody] ExtendExpirationDto dto)
    {
        var returnShipment = await _db.ReturnShipments.FindAsync(id);
        if (returnShipment == null)
            throw new NotFoundException("Return shipment not found");

        returnShipment.ExpiresAt = DateTime.UtcNow.AddDays(dto.Days);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = $"Expiration extended by {dto.Days} days",
            ReturnShipmentId = id,
            ExpiresAt = returnShipment.ExpiresAt
        });
    }

    /// <summary>
    /// Get return shipment statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var returns = await _db.ReturnShipments
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to)
            .ToListAsync();

        var allReturns = await _db.ReturnShipments.ToListAsync();

        var stats = new
        {
            Period = new { From = from, To = to },

            Summary = new
            {
                TotalReturns = returns.Count,
                Pending = returns.Count(r => r.Status == ReturnShipmentStatus.Pending || r.Status == ReturnShipmentStatus.CodeGenerated),
                InTransit = returns.Count(r => r.Status == ReturnShipmentStatus.InTransit),
                Delivered = returns.Count(r => r.Status == ReturnShipmentStatus.Delivered),
                Approved = returns.Count(r => r.Status == ReturnShipmentStatus.Approved),
                Rejected = returns.Count(r => r.Status == ReturnShipmentStatus.Rejected),
                Expired = returns.Count(r => r.Status == ReturnShipmentStatus.Expired)
            },

            ByPaidBy = returns
                .GroupBy(r => r.PaidBy)
                .Select(g => new
                {
                    PaidBy = g.Key.ToString(),
                    Count = g.Count(),
                    TotalCost = g.Sum(r => r.ShippingCost)
                })
                .ToList(),

            ByReason = returns
                .GroupBy(r => r.Reason)
                .Select(g => new
                {
                    Reason = g.Key.ToString(),
                    Count = g.Count(),
                    IsBuyerFault = g.Key.IsBuyerFault(),
                    IsSellerFault = g.Key.IsSellerFault()
                })
                .OrderByDescending(x => x.Count)
                .ToList(),

            CostBreakdown = new
            {
                BuyerPaid = returns.Where(r => r.PaidBy == ShippingPaidBy.Buyer).Sum(r => r.ShippingCost),
                SellerPaid = returns.Where(r => r.PaidBy == ShippingPaidBy.Seller).Sum(r => r.ShippingCost),
                PlatformPaid = returns.Where(r => r.PaidBy == ShippingPaidBy.Platform).Sum(r => r.ShippingCost)
            },

            CurrentPending = allReturns.Count(r =>
                r.Status == ReturnShipmentStatus.Pending ||
                r.Status == ReturnShipmentStatus.CodeGenerated ||
                r.Status == ReturnShipmentStatus.InTransit)
        };

        return Ok(stats);
    }
}

public class CreateReturnShipmentDto
{
    public int? TicketId { get; set; }
    public int? RefundId { get; set; }

    [Required]
    public int SellerOrderId { get; set; }

    public int? CarrierId { get; set; }

    [StringLength(50)]
    public string? CarrierName { get; set; }

    [Required]
    public ReturnReason Reason { get; set; }

    [StringLength(500)]
    public string? ReasonDescription { get; set; }

    public ShippingPaidBy? PaidByOverride { get; set; }

    public bool IsPickupRequested { get; set; } = false;

    [StringLength(500)]
    public string? PickupAddress { get; set; }
}

public class UpdateReturnStatusDto
{
    public ReturnShipmentStatus Status { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class SetReturnTrackingDto
{
    [Required]
    [StringLength(100)]
    public required string TrackingNumber { get; set; }

    public int? CarrierId { get; set; }
}

public class UpdatePaidByDto
{
    public ShippingPaidBy PaidBy { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class ExtendExpirationDto
{
    [Range(1, 30)]
    public int Days { get; set; } = 7;
}

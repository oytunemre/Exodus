using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Models.Enums;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FarmazonDemo.Controllers.Admin;

[Route("api/admin/shipments")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminShipmentController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminShipmentController(ApplicationDbContext db)
    {
        _db = db;
    }

    #region Shipment Management

    /// <summary>
    /// Get all shipments with filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetShipments(
        [FromQuery] ShipmentStatus? status = null,
        [FromQuery] string? carrier = null,
        [FromQuery] int? sellerId = null,
        [FromQuery] string? trackingNumber = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] bool sortDesc = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Shipments
            .Include(s => s.SellerOrder)
                .ThenInclude(so => so.Order)
            .Include(s => s.SellerOrder)
                .ThenInclude(so => so.Seller)
            .AsQueryable();

        // Filters
        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        if (!string.IsNullOrEmpty(carrier))
            query = query.Where(s => s.Carrier.Contains(carrier));

        if (sellerId.HasValue)
            query = query.Where(s => s.SellerOrder.SellerId == sellerId.Value);

        if (!string.IsNullOrEmpty(trackingNumber))
            query = query.Where(s => s.TrackingNumber != null && s.TrackingNumber.Contains(trackingNumber));

        if (fromDate.HasValue)
            query = query.Where(s => s.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(s => s.CreatedAt <= toDate.Value);

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "status" => sortDesc ? query.OrderByDescending(s => s.Status) : query.OrderBy(s => s.Status),
            "shippedat" => sortDesc ? query.OrderByDescending(s => s.ShippedAt) : query.OrderBy(s => s.ShippedAt),
            _ => sortDesc ? query.OrderByDescending(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var shipments = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.Carrier,
                s.TrackingNumber,
                s.Status,
                s.ShippedAt,
                s.DeliveredAt,
                s.CreatedAt,
                SellerOrder = new
                {
                    s.SellerOrder.Id,
                    s.SellerOrder.OrderId,
                    OrderNumber = s.SellerOrder.Order.OrderNumber,
                    s.SellerOrder.SubTotal
                },
                Seller = new
                {
                    s.SellerOrder.Seller.Id,
                    s.SellerOrder.Seller.Name
                }
            })
            .ToListAsync();

        return Ok(new
        {
            Items = shipments,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    /// <summary>
    /// Get shipment details
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetShipment(int id)
    {
        var shipment = await _db.Shipments
            .Include(s => s.SellerOrder)
                .ThenInclude(so => so.Order)
                    .ThenInclude(o => o.Buyer)
            .Include(s => s.SellerOrder)
                .ThenInclude(so => so.Seller)
            .Include(s => s.SellerOrder)
                .ThenInclude(so => so.Items)
            .Where(s => s.Id == id)
            .FirstOrDefaultAsync();

        if (shipment == null)
            throw new NotFoundException("Shipment not found");

        var events = await _db.ShipmentEvents
            .Where(e => e.ShipmentId == id)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        // Get tracking URL if carrier has template
        var carrier = await _db.ShippingCarriers
            .FirstOrDefaultAsync(c => c.Code == shipment.Carrier || c.Name == shipment.Carrier);

        string? trackingUrl = null;
        if (carrier?.TrackingUrlTemplate != null && shipment.TrackingNumber != null)
        {
            trackingUrl = carrier.TrackingUrlTemplate.Replace("{tracking}", shipment.TrackingNumber);
        }

        return Ok(new
        {
            shipment.Id,
            shipment.Carrier,
            shipment.TrackingNumber,
            TrackingUrl = trackingUrl,
            shipment.Status,
            shipment.ShippedAt,
            shipment.DeliveredAt,
            shipment.CreatedAt,
            SellerOrder = new
            {
                shipment.SellerOrder.Id,
                shipment.SellerOrder.OrderId,
                OrderNumber = shipment.SellerOrder.Order.OrderNumber,
                shipment.SellerOrder.SubTotal,
                shipment.SellerOrder.Status,
                Items = shipment.SellerOrder.Items.Select(i => new
                {
                    i.ProductName,
                    i.Quantity,
                    i.UnitPrice,
                    i.LineTotal
                })
            },
            Seller = new
            {
                shipment.SellerOrder.Seller.Id,
                shipment.SellerOrder.Seller.Name,
                shipment.SellerOrder.Seller.Email
            },
            Buyer = new
            {
                shipment.SellerOrder.Order.Buyer.Id,
                shipment.SellerOrder.Order.Buyer.Name,
                shipment.SellerOrder.Order.Buyer.Email
            },
            ShippingAddress = shipment.SellerOrder.Order.ShippingAddressSnapshot,
            Events = events.Select(e => new
            {
                e.ShipmentEventId,
                e.Status,
                e.PayloadJson,
                e.CreatedAt
            })
        });
    }

    /// <summary>
    /// Update shipment status
    /// </summary>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateShipmentStatusDto dto)
    {
        var shipment = await _db.Shipments
            .Include(s => s.SellerOrder)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (shipment == null)
            throw new NotFoundException("Shipment not found");

        var oldStatus = shipment.Status;
        shipment.Status = dto.Status;

        // Update timestamps
        if (dto.Status == ShipmentStatus.Shipped && !shipment.ShippedAt.HasValue)
            shipment.ShippedAt = DateTime.UtcNow;
        else if (dto.Status == ShipmentStatus.Delivered && !shipment.DeliveredAt.HasValue)
            shipment.DeliveredAt = DateTime.UtcNow;

        // Add event
        _db.ShipmentEvents.Add(new ShipmentEvent
        {
            ShipmentId = id,
            Status = dto.Status,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { Description = dto.Description ?? $"Status changed to {dto.Status}", Location = dto.Location })
        });

        // Update seller order status if delivered
        if (dto.Status == ShipmentStatus.Delivered)
        {
            shipment.SellerOrder.Status = SellerOrderStatus.Delivered;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = "Shipment status updated",
            ShipmentId = id,
            OldStatus = oldStatus,
            NewStatus = dto.Status
        });
    }

    /// <summary>
    /// Update tracking number
    /// </summary>
    [HttpPatch("{id:int}/tracking")]
    public async Task<ActionResult> UpdateTracking(int id, [FromBody] UpdateTrackingDto dto)
    {
        var shipment = await _db.Shipments.FindAsync(id);
        if (shipment == null)
            throw new NotFoundException("Shipment not found");

        shipment.TrackingNumber = dto.TrackingNumber;
        if (!string.IsNullOrEmpty(dto.Carrier))
            shipment.Carrier = dto.Carrier;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = "Tracking information updated",
            ShipmentId = id,
            TrackingNumber = dto.TrackingNumber,
            Carrier = shipment.Carrier
        });
    }

    /// <summary>
    /// Get shipment statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var shipments = await _db.Shipments
            .Where(s => s.CreatedAt >= from && s.CreatedAt <= to)
            .ToListAsync();

        var allShipments = await _db.Shipments.ToListAsync();

        var stats = new
        {
            Period = new { From = from, To = to },

            // Period stats
            TotalShipments = shipments.Count,
            ShippedCount = shipments.Count(s => s.Status == ShipmentStatus.Shipped),
            DeliveredCount = shipments.Count(s => s.Status == ShipmentStatus.Delivered),
            PackedCount = shipments.Count(s => s.Status == ShipmentStatus.Packed),

            AverageDeliveryTime = shipments
                .Where(s => s.ShippedAt.HasValue && s.DeliveredAt.HasValue)
                .Select(s => (s.DeliveredAt!.Value - s.ShippedAt!.Value).TotalDays)
                .DefaultIfEmpty(0)
                .Average(),

            // Current status
            CurrentStatus = new
            {
                Pending = allShipments.Count(s => s.Status == ShipmentStatus.Created),
                Packed = allShipments.Count(s => s.Status == ShipmentStatus.Packed),
                Shipped = allShipments.Count(s => s.Status == ShipmentStatus.Shipped),
                Delivered = allShipments.Count(s => s.Status == ShipmentStatus.Delivered),
                Returned = allShipments.Count(s => s.Status == ShipmentStatus.Returned),
                Cancelled = allShipments.Count(s => s.Status == ShipmentStatus.Cancelled)
            },

            // By carrier
            ByCarrier = shipments
                .GroupBy(s => s.Carrier)
                .Select(g => new
                {
                    Carrier = g.Key,
                    Count = g.Count(),
                    Delivered = g.Count(s => s.Status == ShipmentStatus.Delivered)
                })
                .OrderByDescending(x => x.Count)
                .ToList()
        };

        return Ok(stats);
    }

    #endregion

    #region Carrier Management

    /// <summary>
    /// Get all shipping carriers
    /// </summary>
    [HttpGet("carriers")]
    public async Task<ActionResult> GetCarriers([FromQuery] bool? isActive = null)
    {
        var query = _db.ShippingCarriers.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        var carriers = await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Code,
                c.LogoUrl,
                c.Website,
                c.Phone,
                c.IsActive,
                c.SupportsApi,
                c.DefaultRate,
                c.FreeShippingThreshold,
                c.DisplayOrder,
                ShipmentCount = _db.Shipments.Count(s => s.Carrier == c.Code || s.Carrier == c.Name)
            })
            .ToListAsync();

        return Ok(carriers);
    }

    /// <summary>
    /// Get carrier details
    /// </summary>
    [HttpGet("carriers/{id:int}")]
    public async Task<ActionResult> GetCarrier(int id)
    {
        var carrier = await _db.ShippingCarriers.FindAsync(id);
        if (carrier == null)
            throw new NotFoundException("Carrier not found");

        return Ok(carrier);
    }

    /// <summary>
    /// Create new shipping carrier
    /// </summary>
    [HttpPost("carriers")]
    public async Task<ActionResult> CreateCarrier([FromBody] CreateCarrierDto dto)
    {
        // Check code uniqueness
        if (!string.IsNullOrEmpty(dto.Code))
        {
            var exists = await _db.ShippingCarriers.AnyAsync(c => c.Code == dto.Code);
            if (exists)
                throw new BadRequestException("Carrier code already exists");
        }

        var carrier = new ShippingCarrier
        {
            Name = dto.Name,
            Code = dto.Code?.ToUpper(),
            LogoUrl = dto.LogoUrl,
            TrackingUrlTemplate = dto.TrackingUrlTemplate,
            Website = dto.Website,
            Phone = dto.Phone,
            IsActive = dto.IsActive,
            SupportsApi = dto.SupportsApi,
            ApiEndpoint = dto.ApiEndpoint,
            ApiKey = dto.ApiKey,
            DefaultRate = dto.DefaultRate,
            FreeShippingThreshold = dto.FreeShippingThreshold,
            DisplayOrder = dto.DisplayOrder
        };

        _db.ShippingCarriers.Add(carrier);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCarrier), new { id = carrier.Id }, new
        {
            Message = "Carrier created successfully",
            CarrierId = carrier.Id
        });
    }

    /// <summary>
    /// Update shipping carrier
    /// </summary>
    [HttpPut("carriers/{id:int}")]
    public async Task<ActionResult> UpdateCarrier(int id, [FromBody] UpdateCarrierDto dto)
    {
        var carrier = await _db.ShippingCarriers.FindAsync(id);
        if (carrier == null)
            throw new NotFoundException("Carrier not found");

        // Check code uniqueness if changed
        if (!string.IsNullOrEmpty(dto.Code) && dto.Code != carrier.Code)
        {
            var exists = await _db.ShippingCarriers.AnyAsync(c => c.Code == dto.Code && c.Id != id);
            if (exists)
                throw new BadRequestException("Carrier code already exists");
        }

        if (!string.IsNullOrEmpty(dto.Name))
            carrier.Name = dto.Name;

        if (dto.Code != null)
            carrier.Code = dto.Code.ToUpper();

        if (dto.LogoUrl != null)
            carrier.LogoUrl = dto.LogoUrl;

        if (dto.TrackingUrlTemplate != null)
            carrier.TrackingUrlTemplate = dto.TrackingUrlTemplate;

        if (dto.Website != null)
            carrier.Website = dto.Website;

        if (dto.Phone != null)
            carrier.Phone = dto.Phone;

        if (dto.IsActive.HasValue)
            carrier.IsActive = dto.IsActive.Value;

        if (dto.SupportsApi.HasValue)
            carrier.SupportsApi = dto.SupportsApi.Value;

        if (dto.ApiEndpoint != null)
            carrier.ApiEndpoint = dto.ApiEndpoint;

        if (dto.ApiKey != null)
            carrier.ApiKey = dto.ApiKey;

        if (dto.DefaultRate.HasValue)
            carrier.DefaultRate = dto.DefaultRate;

        if (dto.FreeShippingThreshold.HasValue)
            carrier.FreeShippingThreshold = dto.FreeShippingThreshold;

        if (dto.DisplayOrder.HasValue)
            carrier.DisplayOrder = dto.DisplayOrder.Value;

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Carrier updated successfully", CarrierId = id });
    }

    /// <summary>
    /// Delete shipping carrier
    /// </summary>
    [HttpDelete("carriers/{id:int}")]
    public async Task<ActionResult> DeleteCarrier(int id)
    {
        var carrier = await _db.ShippingCarriers.FindAsync(id);
        if (carrier == null)
            throw new NotFoundException("Carrier not found");

        // Check if carrier is in use
        var inUse = await _db.Shipments.AnyAsync(s => s.Carrier == carrier.Code || s.Carrier == carrier.Name);
        if (inUse)
            throw new BadRequestException("Cannot delete carrier that is in use. Deactivate it instead.");

        _db.ShippingCarriers.Remove(carrier);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Carrier deleted", CarrierId = id });
    }

    /// <summary>
    /// Toggle carrier active status
    /// </summary>
    [HttpPatch("carriers/{id:int}/toggle-active")]
    public async Task<ActionResult> ToggleCarrierActive(int id)
    {
        var carrier = await _db.ShippingCarriers.FindAsync(id);
        if (carrier == null)
            throw new NotFoundException("Carrier not found");

        carrier.IsActive = !carrier.IsActive;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = carrier.IsActive ? "Carrier activated" : "Carrier deactivated",
            CarrierId = id,
            IsActive = carrier.IsActive
        });
    }

    #endregion
}

public class UpdateShipmentStatusDto
{
    public ShipmentStatus Status { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }
}

public class UpdateTrackingDto
{
    [Required]
    [StringLength(100)]
    public required string TrackingNumber { get; set; }

    [StringLength(50)]
    public string? Carrier { get; set; }
}

public class CreateCarrierDto
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(50)]
    public string? Code { get; set; }

    [StringLength(500)]
    public string? LogoUrl { get; set; }

    [StringLength(500)]
    public string? TrackingUrlTemplate { get; set; }

    [StringLength(500)]
    public string? Website { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;
    public bool SupportsApi { get; set; } = false;

    [StringLength(500)]
    public string? ApiEndpoint { get; set; }

    [StringLength(200)]
    public string? ApiKey { get; set; }

    [Range(0, 10000)]
    public decimal? DefaultRate { get; set; }

    [Range(0, 100000)]
    public decimal? FreeShippingThreshold { get; set; }

    public int DisplayOrder { get; set; } = 0;
}

public class UpdateCarrierDto
{
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(50)]
    public string? Code { get; set; }

    [StringLength(500)]
    public string? LogoUrl { get; set; }

    [StringLength(500)]
    public string? TrackingUrlTemplate { get; set; }

    [StringLength(500)]
    public string? Website { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    public bool? IsActive { get; set; }
    public bool? SupportsApi { get; set; }

    [StringLength(500)]
    public string? ApiEndpoint { get; set; }

    [StringLength(200)]
    public string? ApiKey { get; set; }

    public decimal? DefaultRate { get; set; }
    public decimal? FreeShippingThreshold { get; set; }
    public int? DisplayOrder { get; set; }
}

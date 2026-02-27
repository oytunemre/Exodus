using Exodus.Data;
using Exodus.Models.Dto.SellerDto;
using Exodus.Models.Dto.Shipment;
using Exodus.Models.Enums;
using Exodus.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Exodus.Controllers;

[ApiController]
[Route("api/seller")]
[Authorize(Roles = "Admin,Seller")]
public class SellerController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public SellerController(ApplicationDbContext db) => _db = db;

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdminOrOwner(int sellerId) =>
        User.IsInRole("Admin") || GetCurrentUserId() == sellerId;

    // 1) Satıcı siparişleri
    // Admin: sellerId path param'ını kullanır. Seller: kendi JWT userId'sini kullanır.
    [HttpGet("{sellerId:int}/orders")]
    public async Task<IActionResult> GetSellerOrders(int sellerId)
    {
        var effectiveSellerId = User.IsInRole("Admin") ? sellerId : GetCurrentUserId();

        if (!IsAdminOrOwner(effectiveSellerId))
            return Forbid();

        var list = await _db.SellerOrders
            .Where(x => x.SellerId == effectiveSellerId)
            .Include(x => x.Items)
            .Include(x => x.Shipment)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var dto = list.Select(so => new SellerOrderListDto
        {
            SellerOrderId = so.Id,
            OrderId = so.OrderId,
            SellerId = so.SellerId,
            Status = so.Status,
            SubTotal = so.SubTotal,
            Items = so.Items.Select(i => new SellerOrderItemDto
            {
                SellerOrderItemId = i.Id,
                ListingId = i.ListingId,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                LineTotal = i.LineTotal
            }).ToList(),
            Shipment = so.Shipment == null ? null : new SellerShipmentDto
            {
                Carrier = so.Shipment.Carrier,
                TrackingNumber = so.Shipment.TrackingNumber,
                Status = so.Shipment.Status,
                ShippedAt = so.Shipment.ShippedAt,
                DeliveredAt = so.Shipment.DeliveredAt
            }
        }).ToList();

        return Ok(dto);
    }

    // 2) Kargoya ver (shipment güncelle)
    [HttpPatch("orders/{sellerOrderId:int}/ship")]
    public async Task<IActionResult> Ship(int sellerOrderId, [FromBody] ShipSellerOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Carrier) || string.IsNullOrWhiteSpace(dto.TrackingNumber))
            throw new BadRequestException("Carrier ve TrackingNumber zorunlu.");

        var so = await _db.SellerOrders
            .Include(x => x.Shipment)
            .FirstOrDefaultAsync(x => x.Id == sellerOrderId);

        if (so is null) throw new NotFoundException("SellerOrder not found.");

        if (!IsAdminOrOwner(so.SellerId))
            return Forbid();

        // Eğer shipment yoksa oluştur
        so.Shipment ??= new Models.Entities.Shipment { SellerOrderId = so.Id };

        // Status kontrol
        if (so.Shipment.Status is ShipmentStatus.Delivered)
            throw new BadRequestException("Teslim edilmiş shipment tekrar ship edilemez.");

        so.Shipment.Carrier = dto.Carrier.Trim();
        so.Shipment.TrackingNumber = dto.TrackingNumber.Trim();
        so.Shipment.Status = ShipmentStatus.Shipped;
        so.Shipment.ShippedAt ??= DateTime.UtcNow;

        so.Status = SellerOrderStatus.Shipped;

        await _db.SaveChangesAsync();
        return Ok();
    }

    // 3) Teslim edildi
    [HttpPatch("orders/{sellerOrderId:int}/deliver")]
    public async Task<IActionResult> Deliver(int sellerOrderId)
    {
        var so = await _db.SellerOrders
            .Include(x => x.Shipment)
            .FirstOrDefaultAsync(x => x.Id == sellerOrderId);

        if (so is null) throw new NotFoundException("SellerOrder not found.");

        if (!IsAdminOrOwner(so.SellerId))
            return Forbid();

        if (so.Shipment is null) throw new BadRequestException("Shipment yok. Önce ship yap.");

        if (so.Shipment.Status != ShipmentStatus.Shipped)
            throw new BadRequestException("Deliver için shipment önce Shipped olmalı.");

        so.Shipment.Status = ShipmentStatus.Delivered;
        so.Shipment.DeliveredAt = DateTime.UtcNow;

        so.Status = SellerOrderStatus.Delivered;

        await _db.SaveChangesAsync();
        return Ok();
    }
}

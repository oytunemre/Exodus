using Exodus.Models.Dto.Shipment;
using Exodus.Services.Shipments;
using Exodus.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Exodus.Controllers;

[ApiController]
[Route("api/shipments")]
[Authorize(Roles = "Admin,Seller")]
public class ShipmentController : ControllerBase
{
    private readonly IShipmentService _shipmentService;

    public ShipmentController(IShipmentService shipmentService)
    {
        _shipmentService = shipmentService;
    }

    // SellerOrder'a bağlı shipment getir
    [HttpGet("seller-orders/{sellerOrderId:int}")]
    public async Task<IActionResult> GetBySellerOrderId(int sellerOrderId, CancellationToken ct)
    {
        try
        {
            return Ok(await _shipmentService.GetBySellerOrderIdAsync(sellerOrderId, GetCurrentUserId(), IsAdmin(), ct));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // Ship
    [HttpPatch("seller-orders/{sellerOrderId:int}/ship")]
    public async Task<IActionResult> Ship(int sellerOrderId, [FromBody] ShipSellerOrderDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _shipmentService.ShipAsync(sellerOrderId, dto, GetCurrentUserId(), IsAdmin(), ct));
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // Deliver
    [HttpPatch("seller-orders/{sellerOrderId:int}/deliver")]
    public async Task<IActionResult> Deliver(int sellerOrderId, CancellationToken ct)
    {
        try
        {
            return Ok(await _shipmentService.DeliverAsync(sellerOrderId, GetCurrentUserId(), IsAdmin(), ct));
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // Timeline (Admin panel için altın endpoint)
    [HttpGet("seller-orders/{sellerOrderId:int}/timeline")]
    public async Task<IActionResult> Timeline(int sellerOrderId, CancellationToken ct)
    {
        try
        {
            return Ok(await _shipmentService.GetTimelineBySellerOrderIdAsync(sellerOrderId, GetCurrentUserId(), IsAdmin(), ct));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
            throw new UnauthorizedException("Invalid user token");
        return userId;
    }

    private bool IsAdmin() => User.IsInRole("Admin");
}

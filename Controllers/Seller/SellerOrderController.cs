using Exodus.Models.Dto;
using Exodus.Models.Dto.OrderDto;
using Exodus.Models.Enums;
using Exodus.Services.Orders;
using Exodus.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Exodus.Controllers.Seller
{
    [Route("api/seller/orders")]
    [ApiController]
    [Authorize(Policy = "SellerOnly")]
    public class SellerOrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public SellerOrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Get seller's orders
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<SellerOrderDto>>> GetOrders(
            [FromQuery] OrderStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var sellerId = GetCurrentUserId();
            var orders = await _orderService.GetSellerOrdersAsync(sellerId, status, page, pageSize);
            return Ok(orders);
        }

        /// <summary>
        /// Update seller order status (Confirm, Ship, etc.)
        /// </summary>
        [HttpPatch("{sellerOrderId:int}/status")]
        public async Task<ActionResult> UpdateStatus(int sellerOrderId, [FromBody] UpdateSellerOrderStatusDto dto)
        {
            var sellerId = GetCurrentUserId();
            await _orderService.UpdateSellerOrderStatusAsync(sellerId, sellerOrderId, dto.Status);
            return Ok(new { Message = "Order status updated successfully" });
        }

        /// <summary>
        /// Confirm seller order
        /// </summary>
        [HttpPost("{sellerOrderId:int}/confirm")]
        public async Task<ActionResult> ConfirmOrder(int sellerOrderId)
        {
            var sellerId = GetCurrentUserId();
            await _orderService.UpdateSellerOrderStatusAsync(sellerId, sellerOrderId, OrderStatus.Confirmed);
            return Ok(new { Message = "Order confirmed successfully" });
        }

        /// <summary>
        /// Mark seller order as shipped
        /// </summary>
        [HttpPost("{sellerOrderId:int}/ship")]
        public async Task<ActionResult> ShipOrder(int sellerOrderId, [FromBody] ShipOrderDto dto)
        {
            var sellerId = GetCurrentUserId();
            // First update status
            await _orderService.UpdateSellerOrderStatusAsync(sellerId, sellerOrderId, OrderStatus.Shipped);
            // TODO: Create shipment with tracking info
            return Ok(new { Message = "Order marked as shipped" });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedException("Invalid user token");
            return userId;
        }
    }

    public class UpdateSellerOrderStatusDto
    {
        public OrderStatus Status { get; set; }
    }

    public class ShipOrderDto
    {
        public string? Carrier { get; set; }
        public string? TrackingNumber { get; set; }
    }
}

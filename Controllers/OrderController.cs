using FarmazonDemo.Models.Dto;
using FarmazonDemo.Models.Dto.OrderDto;
using FarmazonDemo.Models.Enums;
using FarmazonDemo.Services.Orders;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FarmazonDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Create order from cart (checkout)
        /// </summary>
        [HttpPost("checkout")]
        public async Task<ActionResult<OrderDetailResponseDto>> Checkout([FromBody] CreateOrderDto dto)
        {
            var userId = GetCurrentUserId();
            var order = await _orderService.CheckoutAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }

        /// <summary>
        /// Get order by ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<OrderDetailResponseDto>> GetById(int id)
        {
            var userId = GetCurrentUserId();
            var order = await _orderService.GetByIdAsync(userId, id);
            return Ok(order);
        }

        /// <summary>
        /// Get order by order number
        /// </summary>
        [HttpGet("number/{orderNumber}")]
        public async Task<ActionResult<OrderDetailResponseDto>> GetByOrderNumber(string orderNumber)
        {
            var userId = GetCurrentUserId();
            var order = await _orderService.GetByOrderNumberAsync(userId, orderNumber);
            return Ok(order);
        }

        /// <summary>
        /// Get current user's orders
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<OrderListResponseDto>> GetMyOrders(
            [FromQuery] OrderStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var orders = await _orderService.GetUserOrdersAsync(userId, status, page, pageSize);
            return Ok(orders);
        }

        /// <summary>
        /// Cancel order
        /// </summary>
        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult<OrderDetailResponseDto>> CancelOrder(int id, [FromBody] CancelOrderDto dto)
        {
            var userId = GetCurrentUserId();
            var order = await _orderService.CancelOrderAsync(userId, id, dto);
            return Ok(order);
        }

        /// <summary>
        /// Request refund for order
        /// </summary>
        [HttpPost("{id:int}/refund")]
        public async Task<ActionResult<RefundResponseDto>> RequestRefund(int id, [FromBody] RefundRequestDto dto)
        {
            var userId = GetCurrentUserId();
            var refund = await _orderService.RequestRefundAsync(userId, id, dto);
            return Ok(refund);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedException("Invalid user token");
            return userId;
        }
    }
}

using FarmazonDemo.Models.Dto;
using FarmazonDemo.Models.Dto.OrderDto;
using FarmazonDemo.Models.Enums;
using FarmazonDemo.Services.Orders;
using FarmazonDemo.Services.Common;
using FarmazonDemo.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FarmazonDemo.Controllers.Admin
{
    [Route("api/admin/orders")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class AdminOrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ApplicationDbContext _context;

        public AdminOrderController(IOrderService orderService, ApplicationDbContext context)
        {
            _orderService = orderService;
            _context = context;
        }

        /// <summary>
        /// Get all orders (admin)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetAllOrders(
            [FromQuery] OrderStatus? status = null,
            [FromQuery] int? userId = null,
            [FromQuery] string? orderNumber = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Orders.AsQueryable();

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            if (userId.HasValue)
                query = query.Where(o => o.BuyerId == userId.Value);

            if (!string.IsNullOrEmpty(orderNumber))
                query = query.Where(o => o.OrderNumber.Contains(orderNumber));

            if (fromDate.HasValue)
                query = query.Where(o => o.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(o => o.CreatedAt <= toDate.Value);

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderSummaryDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    BuyerId = o.BuyerId,
                    BuyerName = o.Buyer.FirstName + " " + o.Buyer.LastName,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    Currency = o.Currency,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                Items = orders,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// Get order by ID (admin - can see any order)
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Buyer)
                .Include(o => o.SellerOrders)
                    .ThenInclude(so => so.Items)
                        .ThenInclude(i => i.Listing)
                            .ThenInclude(l => l.Product)
                .Include(o => o.OrderEvents)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                throw new NotFoundException("Order not found");

            return Ok(order);
        }

        /// <summary>
        /// Update order status (admin)
        /// </summary>
        [HttpPatch("{id:int}/status")]
        public async Task<ActionResult<OrderDetailResponseDto>> UpdateStatus(int id, [FromBody] AdminUpdateOrderStatusDto dto)
        {
            var adminId = GetCurrentUserId();
            var order = await _orderService.UpdateStatusAsync(id, dto.Status, adminId, dto.Note);
            return Ok(order);
        }

        /// <summary>
        /// Get all refund requests
        /// </summary>
        [HttpGet("refunds")]
        public async Task<ActionResult> GetRefunds(
            [FromQuery] RefundStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Refunds.AsQueryable();

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            var totalCount = await query.CountAsync();

            var refunds = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RefundResponseDto
                {
                    Id = r.Id,
                    RefundNumber = r.RefundNumber,
                    OrderId = r.OrderId,
                    Status = r.Status,
                    Type = r.Type,
                    Reason = r.Reason,
                    Amount = r.Amount,
                    Currency = r.Currency,
                    CreatedAt = r.CreatedAt,
                    ProcessedAt = r.ProcessedAt
                })
                .ToListAsync();

            return Ok(new
            {
                Items = refunds,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// Approve or reject refund
        /// </summary>
        [HttpPost("refunds/{refundId:int}/process")]
        public async Task<ActionResult<RefundResponseDto>> ProcessRefund(int refundId, [FromBody] ProcessRefundDto dto)
        {
            var adminId = GetCurrentUserId();
            var refund = await _orderService.ProcessRefundAsync(refundId, dto.Approve, adminId, dto.Note);
            return Ok(refund);
        }

        /// <summary>
        /// Get order statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult> GetStatistics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            var orders = await _context.Orders
                .Where(o => o.CreatedAt >= from && o.CreatedAt <= to)
                .ToListAsync();

            var statistics = new
            {
                TotalOrders = orders.Count,
                PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending),
                ProcessingOrders = orders.Count(o => o.Status == OrderStatus.Processing),
                CompletedOrders = orders.Count(o => o.Status == OrderStatus.Completed),
                CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled),
                TotalRevenue = orders.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.TotalAmount),
                AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0,
                FromDate = from,
                ToDate = to
            };

            return Ok(statistics);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedException("Invalid user token");
            return userId;
        }
    }

    public class AdminUpdateOrderStatusDto
    {
        public OrderStatus Status { get; set; }
        public string? Note { get; set; }
    }

    public class ProcessRefundDto
    {
        public bool Approve { get; set; }
        public string? Note { get; set; }
    }
}

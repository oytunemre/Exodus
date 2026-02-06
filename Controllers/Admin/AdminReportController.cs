using Exodus.Data;
using Exodus.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Exodus.Controllers.Admin;

[Route("api/admin/reports")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminReportController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminReportController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get sales report
    /// </summary>
    [HttpGet("sales")]
    public async Task<ActionResult> GetSalesReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string groupBy = "day") // day, week, month
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var orders = await _db.Orders
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to)
            .ToListAsync();

        var completedOrders = orders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).ToList();

        // Group by period
        var salesData = groupBy.ToLower() switch
        {
            "week" => completedOrders
                .GroupBy(o => new { Year = o.CreatedAt.Year, Week = GetWeekOfYear(o.CreatedAt) })
                .Select(g => new
                {
                    Period = $"{g.Key.Year}-W{g.Key.Week:D2}",
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount),
                    AverageOrderValue = g.Average(o => o.TotalAmount)
                })
                .OrderBy(x => x.Period)
                .ToList(),
            "month" => completedOrders
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount),
                    AverageOrderValue = g.Average(o => o.TotalAmount)
                })
                .OrderBy(x => x.Period)
                .ToList(),
            _ => completedOrders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Period = g.Key.ToString("yyyy-MM-dd"),
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount),
                    AverageOrderValue = g.Average(o => o.TotalAmount)
                })
                .OrderBy(x => x.Period)
                .ToList()
        };

        // Get commission data
        var commissionSetting = await _db.SiteSettings
            .FirstOrDefaultAsync(s => s.Key == "Commission.DefaultRate");
        var commissionRate = decimal.TryParse(commissionSetting?.Value, out var rate) ? rate : 10m;

        var totalRevenue = completedOrders.Sum(o => o.TotalAmount);
        var totalCommission = totalRevenue * (commissionRate / 100);

        return Ok(new
        {
            Period = new { From = from, To = to },
            Summary = new
            {
                TotalOrders = orders.Count,
                CompletedOrders = completedOrders.Count,
                CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled),
                TotalRevenue = totalRevenue,
                EstimatedCommission = totalCommission,
                AverageOrderValue = completedOrders.Any() ? completedOrders.Average(o => o.TotalAmount) : 0,
                TotalShippingCollected = completedOrders.Sum(o => o.ShippingCost),
                TotalDiscountsGiven = completedOrders.Sum(o => o.DiscountAmount)
            },
            SalesData = salesData
        });
    }

    /// <summary>
    /// Get product performance report
    /// </summary>
    [HttpGet("products")]
    public async Task<ActionResult> GetProductReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] int limit = 50)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var query = _db.SellerOrderItems
            .Include(i => i.SellerOrder)
            .Where(i => i.SellerOrder.CreatedAt >= from && i.SellerOrder.CreatedAt <= to);

        if (categoryId.HasValue)
        {
            var productIds = await _db.Products
                .Where(p => p.CategoryId == categoryId.Value)
                .Select(p => p.Id)
                .ToListAsync();
            query = query.Where(i => productIds.Contains(i.ProductId));
        }

        var productStats = await query
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                TotalQuantitySold = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.LineTotal),
                OrderCount = g.Select(i => i.SellerOrderId).Distinct().Count(),
                AveragePrice = g.Average(i => i.UnitPrice)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(limit)
            .ToListAsync();

        // Get low stock products
        var lowStockProducts = await _db.Listings
            .Include(l => l.Product)
            .Where(l => l.IsActive && l.StockQuantity <= 10)
            .OrderBy(l => l.StockQuantity)
            .Take(20)
            .Select(l => new
            {
                l.ProductId,
                ProductName = l.Product.ProductName,
                l.StockQuantity,
                SellerId = l.SellerId,
                SellerName = _db.Users.Where(u => u.Id == l.SellerId).Select(u => u.Name).FirstOrDefault()
            })
            .ToListAsync();

        return Ok(new
        {
            Period = new { From = from, To = to },
            TopProducts = productStats,
            LowStockProducts = lowStockProducts
        });
    }

    /// <summary>
    /// Get seller performance report
    /// </summary>
    [HttpGet("sellers")]
    public async Task<ActionResult> GetSellerReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int limit = 50)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var sellerStats = await _db.SellerOrders
            .Include(so => so.Seller)
            .Where(so => so.CreatedAt >= from && so.CreatedAt <= to)
            .GroupBy(so => new { so.SellerId, so.Seller.Name, so.Seller.Email })
            .Select(g => new
            {
                SellerId = g.Key.SellerId,
                SellerName = g.Key.Name,
                SellerEmail = g.Key.Email,
                TotalOrders = g.Count(),
                CompletedOrders = g.Count(so => so.Status == SellerOrderStatus.Delivered),
                CancelledOrders = g.Count(so => so.Status == SellerOrderStatus.Cancelled),
                TotalRevenue = g.Where(so => so.Status == SellerOrderStatus.Delivered).Sum(so => so.SubTotal),
                AverageOrderValue = g.Average(so => so.SubTotal)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(limit)
            .ToListAsync();

        // Get commission breakdown
        var commissionSetting = await _db.SiteSettings
            .FirstOrDefaultAsync(s => s.Key == "Commission.DefaultRate");
        var defaultRate = decimal.TryParse(commissionSetting?.Value, out var rate) ? rate : 10m;

        var sellersWithCommission = new List<object>();
        foreach (var seller in sellerStats)
        {
            var profile = await _db.SellerProfiles.FirstOrDefaultAsync(p => p.UserId == seller.SellerId);
            var commissionRate = profile?.CustomCommissionRate ?? defaultRate;
            var commission = seller.TotalRevenue * (commissionRate / 100);

            sellersWithCommission.Add(new
            {
                seller.SellerId,
                seller.SellerName,
                seller.SellerEmail,
                seller.TotalOrders,
                seller.CompletedOrders,
                seller.CancelledOrders,
                seller.TotalRevenue,
                seller.AverageOrderValue,
                CommissionRate = commissionRate,
                CommissionAmount = commission,
                NetEarnings = seller.TotalRevenue - commission
            });
        }

        return Ok(new
        {
            Period = new { From = from, To = to },
            Sellers = sellersWithCommission
        });
    }

    /// <summary>
    /// Get customer report
    /// </summary>
    [HttpGet("customers")]
    public async Task<ActionResult> GetCustomerReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int limit = 50)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var customerStats = await _db.Orders
            .Include(o => o.Buyer)
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to)
            .GroupBy(o => new { o.BuyerId, o.Buyer.Name, o.Buyer.Email })
            .Select(g => new
            {
                CustomerId = g.Key.BuyerId,
                CustomerName = g.Key.Name,
                CustomerEmail = g.Key.Email,
                TotalOrders = g.Count(),
                TotalSpent = g.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount),
                AverageOrderValue = g.Average(o => o.TotalAmount),
                FirstOrderDate = g.Min(o => o.CreatedAt),
                LastOrderDate = g.Max(o => o.CreatedAt)
            })
            .OrderByDescending(x => x.TotalSpent)
            .Take(limit)
            .ToListAsync();

        // Summary stats
        var allCustomers = await _db.Users.Where(u => u.Role == UserRole.Customer).CountAsync();
        var activeCustomers = customerStats.Count;
        var newCustomers = await _db.Users
            .Where(u => u.Role == UserRole.Customer && u.CreatedAt >= from && u.CreatedAt <= to)
            .CountAsync();

        return Ok(new
        {
            Period = new { From = from, To = to },
            Summary = new
            {
                TotalCustomers = allCustomers,
                ActiveCustomers = activeCustomers,
                NewCustomers = newCustomers
            },
            TopCustomers = customerStats
        });
    }

    /// <summary>
    /// Get category performance report
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult> GetCategoryReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var categoryStats = await _db.SellerOrderItems
            .Include(i => i.SellerOrder)
            .Include(i => i.Listing)
                .ThenInclude(l => l.Product)
                    .ThenInclude(p => p.Category)
            .Where(i => i.SellerOrder.CreatedAt >= from && i.SellerOrder.CreatedAt <= to)
            .Where(i => i.Listing != null && i.Listing.Product != null)
            .GroupBy(i => new { i.Listing!.Product!.CategoryId, CategoryName = i.Listing.Product.Category != null ? i.Listing.Product.Category.Name : "Uncategorized" })
            .Select(g => new
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName ?? "Uncategorized",
                TotalQuantitySold = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.LineTotal),
                ProductCount = g.Select(i => i.ProductId).Distinct().Count(),
                OrderCount = g.Select(i => i.SellerOrderId).Distinct().Count()
            })
            .OrderByDescending(x => x.TotalRevenue)
            .ToListAsync();

        return Ok(new
        {
            Period = new { From = from, To = to },
            Categories = categoryStats
        });
    }

    /// <summary>
    /// Get refund report
    /// </summary>
    [HttpGet("refunds")]
    public async Task<ActionResult> GetRefundReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var refunds = await _db.Refunds
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to)
            .ToListAsync();

        var stats = new
        {
            Period = new { From = from, To = to },
            Summary = new
            {
                TotalRefundRequests = refunds.Count,
                ApprovedRefunds = refunds.Count(r => r.Status == RefundStatus.Approved || r.Status == RefundStatus.Completed),
                RejectedRefunds = refunds.Count(r => r.Status == RefundStatus.Rejected),
                PendingRefunds = refunds.Count(r => r.Status == RefundStatus.Pending),
                TotalRefundAmount = refunds.Where(r => r.Status == RefundStatus.Approved || r.Status == RefundStatus.Completed).Sum(r => r.Amount)
            },
            ByReason = refunds
                .GroupBy(r => r.Reason)
                .Select(g => new { Reason = g.Key, Count = g.Count(), Amount = g.Sum(r => r.Amount) })
                .OrderByDescending(x => x.Count)
                .ToList(),
            ByType = refunds
                .GroupBy(r => r.Type)
                .Select(g => new { Type = g.Key.ToString(), Count = g.Count(), Amount = g.Sum(r => r.Amount) })
                .ToList()
        };

        return Ok(stats);
    }

    /// <summary>
    /// Get dashboard overview
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult> GetDashboardOverview()
    {
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
        var lastWeekStart = thisWeekStart.AddDays(-7);
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        // Today's stats
        var todayOrders = await _db.Orders.Where(o => o.CreatedAt >= today).ToListAsync();
        var yesterdayOrders = await _db.Orders.Where(o => o.CreatedAt >= yesterday && o.CreatedAt < today).ToListAsync();

        // This week vs last week
        var thisWeekOrders = await _db.Orders.Where(o => o.CreatedAt >= thisWeekStart).ToListAsync();
        var lastWeekOrders = await _db.Orders.Where(o => o.CreatedAt >= lastWeekStart && o.CreatedAt < thisWeekStart).ToListAsync();

        // This month vs last month
        var thisMonthOrders = await _db.Orders.Where(o => o.CreatedAt >= thisMonthStart).ToListAsync();
        var lastMonthOrders = await _db.Orders.Where(o => o.CreatedAt >= lastMonthStart && o.CreatedAt < thisMonthStart).ToListAsync();

        return Ok(new
        {
            Today = new
            {
                Orders = todayOrders.Count,
                Revenue = todayOrders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount),
                NewCustomers = await _db.Users.CountAsync(u => u.Role == UserRole.Customer && u.CreatedAt >= today)
            },
            Yesterday = new
            {
                Orders = yesterdayOrders.Count,
                Revenue = yesterdayOrders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount)
            },
            ThisWeek = new
            {
                Orders = thisWeekOrders.Count,
                Revenue = thisWeekOrders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount)
            },
            LastWeek = new
            {
                Orders = lastWeekOrders.Count,
                Revenue = lastWeekOrders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount)
            },
            ThisMonth = new
            {
                Orders = thisMonthOrders.Count,
                Revenue = thisMonthOrders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount)
            },
            LastMonth = new
            {
                Orders = lastMonthOrders.Count,
                Revenue = lastMonthOrders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount)
            },
            Pending = new
            {
                Orders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending),
                Refunds = await _db.Refunds.CountAsync(r => r.Status == RefundStatus.Pending),
                SellerApplications = await _db.SellerProfiles.CountAsync(p => p.VerificationStatus == Models.Entities.SellerVerificationStatus.Pending),
                SupportTickets = await _db.SupportTickets.CountAsync(t => t.Status == Models.Entities.TicketStatus.Open || t.Status == Models.Entities.TicketStatus.AwaitingSupport)
            }
        });
    }

    private static int GetWeekOfYear(DateTime date)
    {
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        return cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }
}

using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Controllers.Admin;

[Route("api/admin/invoices")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminInvoiceController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminInvoiceController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> GetInvoices(
        [FromQuery] int? orderId = null,
        [FromQuery] int? userId = null,
        [FromQuery] string? invoiceNumber = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Invoices.Include(i => i.Order).ThenInclude(o => o.Buyer).AsQueryable();

        if (orderId.HasValue) query = query.Where(i => i.OrderId == orderId.Value);
        if (userId.HasValue) query = query.Where(i => i.Order.BuyerId == userId.Value);
        if (!string.IsNullOrEmpty(invoiceNumber)) query = query.Where(i => i.InvoiceNumber.Contains(invoiceNumber));
        if (fromDate.HasValue) query = query.Where(i => i.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(i => i.CreatedAt <= toDate.Value);

        var totalCount = await query.CountAsync();
        var invoices = await query.OrderByDescending(i => i.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new {
                i.Id, i.InvoiceNumber, i.OrderId, OrderNumber = i.Order.OrderNumber,
                i.SubTotal, i.TaxAmount, i.TotalAmount, i.Currency, i.CreatedAt,
                Buyer = new { i.Order.Buyer.Id, i.Order.Buyer.Name, i.Order.Buyer.Email }
            }).ToListAsync();

        return Ok(new { Items = invoices, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetInvoice(int id)
    {
        var invoice = await _db.Invoices.Include(i => i.Order).ThenInclude(o => o.Buyer)
            .Include(i => i.Order).ThenInclude(o => o.SellerOrders).ThenInclude(so => so.Items)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice == null) throw new NotFoundException("Invoice not found");
        return Ok(invoice);
    }

    [HttpGet("by-order/{orderId:int}")]
    public async Task<ActionResult> GetInvoiceByOrder(int orderId)
    {
        var invoice = await _db.Invoices.Include(i => i.Order).FirstOrDefaultAsync(i => i.OrderId == orderId);
        if (invoice == null) throw new NotFoundException("Invoice not found");
        return Ok(invoice);
    }

    [HttpPost("regenerate/{orderId:int}")]
    public async Task<ActionResult> RegenerateInvoice(int orderId)
    {
        var order = await _db.Orders.Include(o => o.Buyer).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) throw new NotFoundException("Order not found");

        var existingInvoice = await _db.Invoices.FirstOrDefaultAsync(i => i.OrderId == orderId);
        if (existingInvoice != null) { _db.Invoices.Remove(existingInvoice); }

        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{orderId:D6}";
        var invoice = new Invoice {
            InvoiceNumber = invoiceNumber, OrderId = orderId,
            SubTotal = order.SubTotal, TaxAmount = order.TaxAmount, TotalAmount = order.TotalAmount,
            Currency = order.Currency, BuyerName = order.Buyer.Name, BuyerAddress = order.BillingAddressSnapshot ?? order.ShippingAddressSnapshot
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Invoice regenerated", InvoiceId = invoice.Id, InvoiceNumber = invoiceNumber });
    }

    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var invoices = await _db.Invoices.Where(i => i.CreatedAt >= from && i.CreatedAt <= to).ToListAsync();

        return Ok(new {
            Period = new { From = from, To = to },
            TotalInvoices = invoices.Count,
            TotalAmount = invoices.Sum(i => i.TotalAmount),
            TotalTax = invoices.Sum(i => i.TaxAmount),
            AverageAmount = invoices.Any() ? invoices.Average(i => i.TotalAmount) : 0
        });
    }
}

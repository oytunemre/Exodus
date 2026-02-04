using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto;
using FarmazonDemo.Models.Dto.OrderDto;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Models.Enums;
using FarmazonDemo.Services.Common;
using FarmazonDemo.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Services.Orders
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notificationService;

        public OrderService(ApplicationDbContext db, INotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
        }

        public async Task<OrderDetailResponseDto> CheckoutAsync(int userId, CreateOrderDto dto)
        {
            var cart = await _db.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Listing)
                        .ThenInclude(l => l.Product)
                            .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart is null || cart.Items.Count == 0)
                throw new BadRequestException("Sepet boş. Checkout yapılamaz.");

            var buyer = await _db.Users.FindAsync(userId);
            if (buyer == null) throw new NotFoundException("User not found.");

            // Validate shipping address
            var shippingAddress = await _db.Addresses
                .FirstOrDefaultAsync(a => a.Id == dto.ShippingAddressId && a.UserId == userId);
            if (shippingAddress == null)
                throw new BadRequestException("Geçerli bir teslimat adresi seçiniz.");

            Address? billingAddress = null;
            if (dto.BillingAddressId.HasValue)
            {
                billingAddress = await _db.Addresses
                    .FirstOrDefaultAsync(a => a.Id == dto.BillingAddressId && a.UserId == userId);
            }

            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync();
                try
                {
                    // Create Order
                    var order = new Order
                    {
                        OrderNumber = GenerateOrderNumber(),
                        BuyerId = userId,
                        Status = OrderStatus.Pending,
                        ShippingAddressId = dto.ShippingAddressId,
                        ShippingAddressSnapshot = FormatAddressSnapshot(shippingAddress),
                        BillingAddressId = dto.BillingAddressId,
                        BillingAddressSnapshot = billingAddress != null ? FormatAddressSnapshot(billingAddress) : null,
                        CustomerNote = dto.CustomerNote
                    };
                    _db.Orders.Add(order);
                    await _db.SaveChangesAsync();

                    // Group by seller
                    var groups = cart.Items.GroupBy(i => i.Listing.SellerId);
                    decimal totalSubTotal = 0;

                    foreach (var g in groups)
                    {
                        var sellerId = g.Key;

                        var sellerOrder = new SellerOrder
                        {
                            OrderId = order.Id,
                            SellerId = sellerId,
                            Status = SellerOrderStatus.Placed
                        };

                        foreach (var cartItem in g)
                        {
                            var listing = await _db.Listings
                                .Include(l => l.Product)
                                    .ThenInclude(p => p.Images)
                                .FirstOrDefaultAsync(l => l.Id == cartItem.ListingId);

                            if (listing is null)
                                throw new NotFoundException($"Listing not found. ListingId={cartItem.ListingId}");

                            if (!listing.IsActive)
                                throw new BadRequestException($"Listing aktif değil. ListingId={listing.Id}");

                            if (listing.StockQuantity < cartItem.Quantity)
                                throw new ConflictException($"Yetersiz stok. ListingId={listing.Id}");

                            // Reduce stock
                            listing.StockQuantity -= cartItem.Quantity;
                            if (listing.StockQuantity == 0)
                            {
                                listing.StockStatus = StockStatus.OutOfStock;
                                listing.IsActive = false;
                            }
                            else if (listing.StockQuantity <= listing.LowStockThreshold)
                            {
                                listing.StockStatus = StockStatus.LowStock;
                            }

                            var unitPrice = listing.Price;
                            var lineTotal = unitPrice * cartItem.Quantity;

                            sellerOrder.Items.Add(new SellerOrderItem
                            {
                                ListingId = listing.Id,
                                ProductId = listing.ProductId,
                                ProductName = listing.Product.ProductName,
                                UnitPrice = unitPrice,
                                Quantity = cartItem.Quantity,
                                LineTotal = lineTotal
                            });
                        }

                        sellerOrder.SubTotal = sellerOrder.Items.Sum(x => x.LineTotal);
                        totalSubTotal += sellerOrder.SubTotal;

                        sellerOrder.Shipment = new Shipment
                        {
                            Status = ShipmentStatus.Created
                        };

                        order.SellerOrders.Add(sellerOrder);
                    }

                    order.SubTotal = totalSubTotal;
                    order.TotalAmount = totalSubTotal + order.ShippingCost + order.TaxAmount - order.DiscountAmount;

                    // Clear cart
                    _db.CartItems.RemoveRange(cart.Items);

                    // Add order event
                    order.OrderEvents.Add(new OrderEvent
                    {
                        OrderId = order.Id,
                        Status = OrderStatus.Pending,
                        Title = "Sipariş Oluşturuldu",
                        Description = "Siparişiniz başarıyla oluşturuldu. Ödeme bekleniyor.",
                        UserId = userId,
                        UserType = "Customer"
                    });

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();

                    // Send notification
                    await _notificationService.SendOrderUpdateAsync(
                        userId, order.Id,
                        "Siparişiniz Oluşturuldu",
                        $"#{order.OrderNumber} numaralı siparişiniz oluşturuldu.");

                    return await GetByIdAsync(userId, order.Id);
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<OrderDetailResponseDto> GetByIdAsync(int userId, int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.SellerOrders)
                    .ThenInclude(so => so.Items)
                .Include(o => o.SellerOrders)
                    .ThenInclude(so => so.Shipment)
                .Include(o => o.SellerOrders)
                    .ThenInclude(so => so.Seller)
                .Include(o => o.OrderEvents)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.BuyerId == userId);

            if (order is null)
                throw new NotFoundException("Sipariş bulunamadı.");

            return MapToDetailDto(order);
        }

        public async Task<OrderDetailResponseDto> GetByOrderNumberAsync(int userId, string orderNumber)
        {
            var order = await _db.Orders
                .Include(o => o.SellerOrders)
                    .ThenInclude(so => so.Items)
                .Include(o => o.SellerOrders)
                    .ThenInclude(so => so.Shipment)
                .Include(o => o.OrderEvents)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber && o.BuyerId == userId);

            if (order is null)
                throw new NotFoundException("Sipariş bulunamadı.");

            return MapToDetailDto(order);
        }

        public async Task<OrderListResponseDto> GetUserOrdersAsync(int userId, OrderStatus? status = null, int page = 1, int pageSize = 10)
        {
            var query = _db.Orders
                .Where(o => o.BuyerId == userId);

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(o => o.SellerOrders)
                    .ThenInclude(so => so.Items)
                .ToListAsync();

            return new OrderListResponseDto
            {
                Items = orders.Select(o => new OrderListItemDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    Currency = o.Currency,
                    ItemCount = o.SellerOrders.Sum(so => so.Items.Sum(i => i.Quantity)),
                    CreatedAt = o.CreatedAt
                }).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<OrderDetailResponseDto> UpdateStatusAsync(int orderId, OrderStatus newStatus, int? userId = null, string? note = null)
        {
            var order = await _db.Orders
                .Include(o => o.OrderEvents)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                throw new NotFoundException("Sipariş bulunamadı.");

            var oldStatus = order.Status;
            order.Status = newStatus;

            // Update timestamps
            switch (newStatus)
            {
                case OrderStatus.Processing:
                    order.PaidAt = DateTime.UtcNow;
                    break;
                case OrderStatus.Shipped:
                    order.ShippedAt = DateTime.UtcNow;
                    break;
                case OrderStatus.Delivered:
                    order.DeliveredAt = DateTime.UtcNow;
                    break;
                case OrderStatus.Completed:
                    order.CompletedAt = DateTime.UtcNow;
                    break;
            }

            await AddOrderEventAsync(orderId, newStatus, GetStatusTitle(newStatus), note, userId, userId.HasValue ? "Admin" : "System");
            await _db.SaveChangesAsync();

            // Send notification to buyer
            await _notificationService.SendOrderUpdateAsync(
                order.BuyerId, orderId,
                GetStatusTitle(newStatus),
                $"#{order.OrderNumber} numaralı siparişinizin durumu güncellendi.");

            return await GetByIdAsync(order.BuyerId, orderId);
        }

        public async Task<OrderDetailResponseDto> CancelOrderAsync(int userId, int orderId, CancelOrderDto dto)
        {
            var order = await _db.Orders
                .Include(o => o.SellerOrders)
                    .ThenInclude(so => so.Items)
                        .ThenInclude(i => i.Listing)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.BuyerId == userId);

            if (order == null)
                throw new NotFoundException("Sipariş bulunamadı.");

            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
                throw new BadRequestException("Bu sipariş iptal edilemez.");

            // Restore stock
            foreach (var so in order.SellerOrders)
            {
                foreach (var item in so.Items)
                {
                    if (item.Listing != null)
                    {
                        item.Listing.StockQuantity += item.Quantity;
                        if (item.Listing.StockQuantity > 0)
                        {
                            item.Listing.StockStatus = item.Listing.StockQuantity <= item.Listing.LowStockThreshold
                                ? StockStatus.LowStock
                                : StockStatus.InStock;
                            item.Listing.IsActive = true;
                        }
                    }
                }
            }

            order.Status = OrderStatus.Cancelled;
            order.CancellationReason = dto.Reason;
            order.CancellationNote = dto.Note;
            order.CancelledAt = DateTime.UtcNow;

            await AddOrderEventAsync(orderId, OrderStatus.Cancelled, "Sipariş İptal Edildi", dto.Note, userId, "Customer");
            await _db.SaveChangesAsync();

            await _notificationService.SendOrderUpdateAsync(
                userId, orderId,
                "Siparişiniz İptal Edildi",
                $"#{order.OrderNumber} numaralı siparişiniz iptal edildi.");

            return await GetByIdAsync(userId, orderId);
        }

        public async Task<RefundResponseDto> RequestRefundAsync(int userId, int orderId, RefundRequestDto dto)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.BuyerId == userId);
            if (order == null)
                throw new NotFoundException("Sipariş bulunamadı.");

            if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Completed)
                throw new BadRequestException("Sadece teslim edilmiş siparişler için iade talebinde bulunabilirsiniz.");

            var refundAmount = dto.Amount ?? order.TotalAmount;

            var refund = new Refund
            {
                RefundNumber = GenerateRefundNumber(),
                OrderId = orderId,
                SellerOrderId = dto.SellerOrderId,
                Status = RefundStatus.Pending,
                Type = dto.Amount.HasValue && dto.Amount < order.TotalAmount ? RefundType.Partial : RefundType.Full,
                Reason = dto.Reason,
                Description = dto.Description,
                Amount = refundAmount,
                Currency = order.Currency
            };

            _db.Refunds.Add(refund);
            await _db.SaveChangesAsync();

            await _notificationService.SendOrderUpdateAsync(
                userId, orderId,
                "İade Talebiniz Alındı",
                $"#{order.OrderNumber} siparişi için iade talebiniz alındı.");

            return new RefundResponseDto
            {
                Id = refund.Id,
                RefundNumber = refund.RefundNumber,
                Status = refund.Status,
                Type = refund.Type,
                Amount = refund.Amount,
                Currency = refund.Currency,
                Reason = refund.Reason,
                CreatedAt = refund.CreatedAt
            };
        }

        public async Task<RefundResponseDto> ProcessRefundAsync(int refundId, bool approve, int adminUserId, string? note = null)
        {
            var refund = await _db.Refunds
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.Id == refundId);

            if (refund == null)
                throw new NotFoundException("İade talebi bulunamadı.");

            if (refund.Status != RefundStatus.Pending)
                throw new BadRequestException("Bu iade talebi zaten işlenmiş.");

            refund.ProcessedByUserId = adminUserId;
            refund.ProcessedAt = DateTime.UtcNow;

            if (approve)
            {
                refund.Status = RefundStatus.Approved;
                refund.AdminNote = note;

                // Update order status
                var newOrderStatus = refund.Type == RefundType.Full ? OrderStatus.Refunded : OrderStatus.PartialRefund;
                refund.Order.Status = newOrderStatus;

                await _notificationService.SendOrderUpdateAsync(
                    refund.Order.BuyerId, refund.OrderId,
                    "İade Talebiniz Onaylandı",
                    $"#{refund.RefundNumber} numaralı iade talebiniz onaylandı.");
            }
            else
            {
                refund.Status = RefundStatus.Rejected;
                refund.RejectionReason = note;

                await _notificationService.SendOrderUpdateAsync(
                    refund.Order.BuyerId, refund.OrderId,
                    "İade Talebiniz Reddedildi",
                    $"#{refund.RefundNumber} numaralı iade talebiniz reddedildi.");
            }

            await _db.SaveChangesAsync();

            return new RefundResponseDto
            {
                Id = refund.Id,
                RefundNumber = refund.RefundNumber,
                Status = refund.Status,
                Type = refund.Type,
                Amount = refund.Amount,
                Currency = refund.Currency,
                Reason = refund.Reason,
                CreatedAt = refund.CreatedAt,
                ProcessedAt = refund.ProcessedAt
            };
        }

        public async Task<List<SellerOrderDto>> GetSellerOrdersAsync(int sellerId, OrderStatus? status = null, int page = 1, int pageSize = 20)
        {
            var query = _db.SellerOrders
                .Where(so => so.SellerId == sellerId)
                .Include(so => so.Order)
                .Include(so => so.Items)
                .Include(so => so.Shipment)
                .AsQueryable();

            if (status.HasValue)
            {
                var sellerStatus = MapOrderStatusToSellerStatus(status.Value);
                query = query.Where(so => so.Status == sellerStatus);
            }

            var sellerOrders = await query
                .OrderByDescending(so => so.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return sellerOrders.Select(so => new SellerOrderDto
            {
                Id = so.Id,
                SellerId = so.SellerId,
                SellerName = "",
                Status = MapSellerStatusToOrderStatus(so.Status),
                Total = so.SubTotal,
                Items = so.Items.Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.LineTotal
                }).ToList()
            }).ToList();
        }

        public async Task UpdateSellerOrderStatusAsync(int sellerId, int sellerOrderId, OrderStatus newStatus)
        {
            var sellerOrder = await _db.SellerOrders
                .Include(so => so.Order)
                .FirstOrDefaultAsync(so => so.Id == sellerOrderId && so.SellerId == sellerId);

            if (sellerOrder == null)
                throw new NotFoundException("Satıcı siparişi bulunamadı.");

            sellerOrder.Status = MapOrderStatusToSellerStatus(newStatus);
            await _db.SaveChangesAsync();
        }

        public async Task AddOrderEventAsync(int orderId, OrderStatus status, string title, string? description = null, int? userId = null, string? userType = null)
        {
            var orderEvent = new OrderEvent
            {
                OrderId = orderId,
                Status = status,
                Title = title,
                Description = description,
                UserId = userId,
                UserType = userType
            };

            _db.OrderEvents.Add(orderEvent);
            await _db.SaveChangesAsync();
        }

        public string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }

        private string GenerateRefundNumber()
        {
            return $"REF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }

        private static string FormatAddressSnapshot(Address address)
        {
            return $"{address.FullName}, {address.AddressLine}, {address.Neighborhood ?? ""} {address.District}/{address.City} {address.PostalCode ?? ""} - Tel: {address.Phone}";
        }

        private static string GetStatusTitle(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Sipariş Beklemede",
                OrderStatus.Processing => "Sipariş Hazırlanıyor",
                OrderStatus.Confirmed => "Sipariş Onaylandı",
                OrderStatus.Shipped => "Sipariş Kargoya Verildi",
                OrderStatus.Delivered => "Sipariş Teslim Edildi",
                OrderStatus.Completed => "Sipariş Tamamlandı",
                OrderStatus.Cancelled => "Sipariş İptal Edildi",
                OrderStatus.Refunded => "Sipariş İade Edildi",
                _ => "Sipariş Güncellendi"
            };
        }

        private static SellerOrderStatus MapOrderStatusToSellerStatus(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => SellerOrderStatus.Placed,
                OrderStatus.Processing => SellerOrderStatus.Confirmed,
                OrderStatus.Shipped => SellerOrderStatus.Shipped,
                OrderStatus.Delivered => SellerOrderStatus.Delivered,
                OrderStatus.Cancelled => SellerOrderStatus.Cancelled,
                _ => SellerOrderStatus.Placed
            };
        }

        private static OrderStatus MapSellerStatusToOrderStatus(SellerOrderStatus status)
        {
            return status switch
            {
                SellerOrderStatus.Placed => OrderStatus.Pending,
                SellerOrderStatus.Confirmed => OrderStatus.Processing,
                SellerOrderStatus.Shipped => OrderStatus.Shipped,
                SellerOrderStatus.Delivered => OrderStatus.Delivered,
                SellerOrderStatus.Cancelled => OrderStatus.Cancelled,
                _ => OrderStatus.Pending
            };
        }

        private static OrderDetailResponseDto MapToDetailDto(Order order)
        {
            return new OrderDetailResponseDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                SubTotal = order.SubTotal,
                ShippingCost = order.ShippingCost,
                TaxAmount = order.TaxAmount,
                DiscountAmount = order.DiscountAmount,
                TotalAmount = order.TotalAmount,
                Currency = order.Currency,
                ShippingAddress = order.ShippingAddressSnapshot,
                BillingAddress = order.BillingAddressSnapshot,
                CustomerNote = order.CustomerNote,
                CreatedAt = order.CreatedAt,
                PaidAt = order.PaidAt,
                ShippedAt = order.ShippedAt,
                DeliveredAt = order.DeliveredAt,
                SellerOrders = order.SellerOrders.Select(so => new SellerOrderDto
                {
                    Id = so.Id,
                    SellerId = so.SellerId,
                    SellerName = so.Seller?.Name ?? "",
                    Status = MapSellerStatusToOrderStatus(so.Status),
                    Total = so.SubTotal,
                    Items = so.Items.Select(i => new OrderItemDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        TotalPrice = i.LineTotal
                    }).ToList(),
                    Shipment = so.Shipment == null ? null : new ShipmentInfoDto
                    {
                        Id = so.Shipment.Id,
                        Carrier = so.Shipment.Carrier,
                        TrackingNumber = so.Shipment.TrackingNumber,
                        ShippedAt = so.Shipment.ShippedAt,
                        DeliveredAt = so.Shipment.DeliveredAt
                    }
                }).ToList(),
                Events = order.OrderEvents.OrderByDescending(e => e.CreatedAt).Select(e => new OrderEventDto
                {
                    Id = e.Id,
                    Status = e.Status,
                    Title = e.Title,
                    Description = e.Description,
                    CreatedAt = e.CreatedAt
                }).ToList()
            };
        }
    }
}

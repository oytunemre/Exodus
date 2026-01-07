using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Models.Enums;
using FarmazonDemo.Services.Common;
using Microsoft.EntityFrameworkCore;
using FarmazonDemo.Models.Dto.OrderDto;
using FarmazonDemo.Models.Dto.OrderDto.SellerOrders;


namespace FarmazonDemo.Services.Orders
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _db;

        public OrderService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<OrderResponseDto> CheckoutAsync(int userId)
        {
            var cart = await _db.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Listing)
                        .ThenInclude(l => l.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart is null || cart.Items.Count == 0)
                throw new BadRequestException("Sepet boş. Checkout yapılamaz.");

            var buyerExists = await _db.Users.AnyAsync(u => u.Id == userId);
            if (!buyerExists) throw new NotFoundException("User not found.");

            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync();
                try
                {
                    // 1) ORDER (buyer)
                    var order = new Order
                    {
                        BuyerId = userId,
                        Status = OrderStatus.Placed
                    };
                    _db.Orders.Add(order);
                    await _db.SaveChangesAsync(); // order.Id lazim

                    // 2) Seller'a göre grupla
                    var groups = cart.Items.GroupBy(i => i.Listing.SellerId);

                    foreach (var g in groups)
                    {
                        var sellerId = g.Key;

                        // SellerOrder
                        var sellerOrder = new SellerOrder
                        {
                            OrderId = order.Id,
                            SellerId = sellerId,
                            Status = SellerOrderStatus.Placed
                        };

                        foreach (var cartItem in g)
                        {
                            // Listing'i DB'den kilitleyerek tekrar çek (stok/fiyat güvenliği)
                            var listing = await _db.Listings
                                .Include(l => l.Product)
                                .FirstOrDefaultAsync(l => l.Id == cartItem.ListingId);

                            if (listing is null)
                                throw new NotFoundException($"Listing not found. ListingId={cartItem.ListingId}");

                            if (!listing.IsActive)
                                throw new BadRequestException($"Listing aktif değil. ListingId={listing.Id}");

                            if (listing.Stock < cartItem.Quantity)
                                throw new ConflictException($"Yetersiz stok. ListingId={listing.Id}, Stock={listing.Stock}, Wanted={cartItem.Quantity}");

                            // stok düş
                            listing.Stock -= cartItem.Quantity;
                            if (listing.Stock == 0) listing.IsActive = false;

                            var unitPrice = listing.Price; // snapshot
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

                        // Shipment şimdilik boş; satıcı kargoya verince dolduracağız
                        sellerOrder.Shipment = new Shipment
                        {
                            Status = ShipmentStatus.Created
                        };

                        order.SellerOrders.Add(sellerOrder);
                    }

                    order.Total = order.SellerOrders.Sum(so => so.SubTotal);

                    // 3) Sepeti boşalt
                    _db.CartItems.RemoveRange(cart.Items);

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();

                    // 4) Return için full load
                    var created = await _db.Orders
                        .Include(o => o.SellerOrders)
                            .ThenInclude(so => so.Items)
                        .Include(o => o.SellerOrders)
                            .ThenInclude(so => so.Shipment)
                        .FirstAsync(o => o.Id == order.Id);

                    return Map(created);
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<OrderResponseDto> GetByIdAsync(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.SellerOrders)
                    .ThenInclude(so => so.Items)
                .Include(o => o.SellerOrders)
                    .ThenInclude(so => so.Shipment)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order is null) throw new NotFoundException("Order not found.");
            return Map(order);
        }

        public async Task<List<OrderResponseDto>> GetByUserAsync(int userId)
        {
            var orders = await _db.Orders
                .Where(o => o.BuyerId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Include(o => o.SellerOrders)
                    .ThenInclude(so => so.Items)
                .Include(o => o.SellerOrders)
                    .ThenInclude(so => so.Shipment)
                .ToListAsync();

            return orders.Select(Map).ToList();
        }

        private static OrderResponseDto Map(Order order)
        {
            return new OrderResponseDto
            {
                OrderId = order.Id,
                BuyerId = order.BuyerId,
                Status = order.Status,
                Total = order.Total,
                SellerOrders = order.SellerOrders.Select(so => new SellerOrderResponseDto
                {
                    SellerOrderId = so.Id,
                    SellerId = so.SellerId,
                    Status = so.Status,
                    SubTotal = so.SubTotal,
                    Items = so.Items.Select(i => new SellerOrderItemResponseDto
                    {
                        SellerOrderItemId = i.Id,
                        ListingId = i.ListingId,
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        UnitPrice = i.UnitPrice,
                        Quantity = i.Quantity,
                        LineTotal = i.LineTotal
                    }).ToList(),
                    Shipment = so.Shipment is null ? null : new SellerShipmentResponseDto
                    {
                        Carrier = so.Shipment.Carrier,
                        TrackingNumber = so.Shipment.TrackingNumber,
                        Status = so.Shipment.Status,
                        ShippedAt = so.Shipment.ShippedAt,
                        DeliveredAt = so.Shipment.DeliveredAt
                    }
                }).ToList()
            };
        }
    }
}

using FarmazonDemo.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FarmazonDemo.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Users> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductBarcode> ProductBarcodes { get; set; }
        public DbSet<Listing> Listings { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<SellerOrder> SellerOrders { get; set; }
        public DbSet<SellerOrderItem> SellerOrderItems { get; set; }
        public DbSet<PaymentIntent> PaymentIntents => Set<PaymentIntent>();
        public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();
        public DbSet<Shipment> Shipments => Set<Shipment>();
        public DbSet<ShipmentEvent> ShipmentEvents => Set<ShipmentEvent>();
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationPreferences> NotificationPreferences { get; set; }
        public DbSet<OrderEvent> OrderEvents { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Refund> Refunds { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<CampaignProduct> CampaignProducts { get; set; }
        public DbSet<CampaignCategory> CampaignCategories { get; set; }
        public DbSet<CampaignUsage> CampaignUsages { get; set; }
        public DbSet<Banner> Banners { get; set; }
        public DbSet<SiteSetting> SiteSettings { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<SupportTicketMessage> SupportTicketMessages { get; set; }
        public DbSet<SellerProfile> SellerProfiles { get; set; }
        public DbSet<ShippingCarrier> ShippingCarriers { get; set; }
        public DbSet<StaticPage> StaticPages { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Soft delete global query filter (BaseEntity olan her şeye) ---
            ApplySoftDeleteQueryFilters(modelBuilder);

            // Cart -> User
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem -> Cart
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem -> Listing
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Listing)
                .WithMany()
                .HasForeignKey(ci => ci.ListingId)
                .OnDelete(DeleteBehavior.Restrict);

            // CartItem unique (soft delete ile çakışmaması için FILTER)
            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => new { ci.CartId, ci.ListingId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            // ProductBarcode -> Product
            modelBuilder.Entity<ProductBarcode>()
                .HasOne(pb => pb.Product)
                .WithMany(p => p.Barcodes)
                .HasForeignKey(pb => pb.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Barcode unique (soft delete ile çakışmaması için FILTER)
            modelBuilder.Entity<ProductBarcode>()
                .HasIndex(pb => pb.Barcode)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            // Listing Condition enum to string
            modelBuilder.Entity<Listing>()
               .Property(l => l.Condition)
               .HasConversion<string>()
                  .HasMaxLength(30);

            // Order -> Buyer
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Buyer)
                .WithMany()
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            // Order -> SellerOrders
            modelBuilder.Entity<SellerOrder>()
                .HasOne(so => so.Order)
                .WithMany(o => o.SellerOrders)
                .HasForeignKey(so => so.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SellerOrder>()
                .HasOne(so => so.Seller)
                .WithMany()
                .HasForeignKey(so => so.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SellerOrder>()
                .Property(so => so.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            // SellerOrderItem -> SellerOrder
            modelBuilder.Entity<SellerOrderItem>()
                .HasOne(i => i.SellerOrder)
                .WithMany(so => so.Items)
                .HasForeignKey(i => i.SellerOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // SellerOrderItem -> Listing
            modelBuilder.Entity<SellerOrderItem>()
                .HasOne(i => i.Listing)
                .WithMany()
                .HasForeignKey(i => i.ListingId)
                .OnDelete(DeleteBehavior.Restrict);

            // PAYMENT
            modelBuilder.Entity<PaymentIntent>(b =>
            {
                b.HasKey(x => x.Id);

                b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
                b.Property(x => x.Currency).HasMaxLength(3);

                b.Property(x => x.Provider).HasMaxLength(50);
                b.Property(x => x.ExternalReference).HasMaxLength(100);
                b.Property(x => x.FailureReason).HasMaxLength(500);

                b.HasOne(x => x.Order)
                    .WithMany()
                    .HasForeignKey(x => x.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 1 order = 1 intent (MVP)
                b.HasIndex(x => x.OrderId).IsUnique();
            });

            modelBuilder.Entity<PaymentEvent>(b =>
            {
                b.HasKey(x => x.Id);

                b.HasOne(x => x.PaymentIntent)
                    .WithMany()
                    .HasForeignKey(x => x.PaymentIntentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            // SHIPMENT
            modelBuilder.Entity<Shipment>(b =>
            {
                b.HasKey(x => x.Id);

                b.Property(x => x.Carrier).HasMaxLength(50);
                b.Property(x => x.TrackingNumber).HasMaxLength(80);

                b.HasOne(x => x.SellerOrder)
                    .WithOne(x => x.Shipment)
                    .HasForeignKey<Shipment>(x => x.SellerOrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => x.SellerOrderId).IsUnique();
                b.HasIndex(x => x.TrackingNumber);

                b.HasQueryFilter(x => !x.IsDeleted);
            });

            modelBuilder.Entity<ShipmentEvent>(b =>
            {
                b.HasKey(x => x.ShipmentEventId);

                b.HasOne(x => x.Shipment)
                    .WithMany()
                    .HasForeignKey(x => x.ShipmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ✅ Matching filter: Shipment gizlenirse event de gizlensin
                b.HasQueryFilter(e => !e.Shipment.IsDeleted);
            });

            // RefreshToken -> User
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // CATEGORY
            modelBuilder.Entity<Category>(b =>
            {
                b.HasIndex(c => c.Slug).IsUnique().HasFilter("[IsDeleted] = 0");

                b.HasOne(c => c.ParentCategory)
                    .WithMany(c => c.SubCategories)
                    .HasForeignKey(c => c.ParentCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Product -> Category
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // ProductImage -> Product
            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Listing StockStatus enum to string
            modelBuilder.Entity<Listing>()
                .Property(l => l.StockStatus)
                .HasConversion<string>()
                .HasMaxLength(30);

            // Address -> User
            modelBuilder.Entity<Address>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Address>()
                .Property(a => a.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Notification -> User
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .Property(n => n.Type)
                .HasConversion<string>()
                .HasMaxLength(30);

            // NotificationPreferences -> User (one-to-one)
            modelBuilder.Entity<NotificationPreferences>()
                .HasOne(np => np.User)
                .WithOne()
                .HasForeignKey<NotificationPreferences>(np => np.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NotificationPreferences>()
                .HasIndex(np => np.UserId)
                .IsUnique();

            // OrderEvent -> Order
            modelBuilder.Entity<OrderEvent>()
                .HasOne(oe => oe.Order)
                .WithMany(o => o.OrderEvents)
                .HasForeignKey(oe => oe.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderEvent>()
                .Property(oe => oe.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            // Invoice -> Order
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Order)
                .WithMany()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.SellerOrder)
                .WithMany()
                .HasForeignKey(i => i.SellerOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<Invoice>()
                .Property(i => i.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Refund -> Order
            modelBuilder.Entity<Refund>()
                .HasOne(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Refund>()
                .HasOne(r => r.SellerOrder)
                .WithMany()
                .HasForeignKey(r => r.SellerOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Refund>()
                .HasIndex(r => r.RefundNumber)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<Refund>()
                .Property(r => r.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Refund>()
                .Property(r => r.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Refund>()
                .Property(r => r.Method)
                .HasConversion<string>()
                .HasMaxLength(30);

            // CAMPAIGN
            modelBuilder.Entity<Campaign>(b =>
            {
                b.HasOne(c => c.Seller)
                    .WithMany()
                    .HasForeignKey(c => c.SellerId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.Property(c => c.Type)
                    .HasConversion<string>()
                    .HasMaxLength(30);

                b.Property(c => c.Scope)
                    .HasConversion<string>()
                    .HasMaxLength(30);

                b.HasIndex(c => c.CouponCode)
                    .IsUnique()
                    .HasFilter("[CouponCode] IS NOT NULL AND [IsDeleted] = 0");

                b.Property(c => c.DiscountPercentage).HasColumnType("decimal(5,2)");
                b.Property(c => c.DiscountAmount).HasColumnType("decimal(18,2)");
                b.Property(c => c.MaxDiscountAmount).HasColumnType("decimal(18,2)");
                b.Property(c => c.MinimumOrderAmount).HasColumnType("decimal(18,2)");
            });

            // CampaignProduct
            modelBuilder.Entity<CampaignProduct>(b =>
            {
                b.HasOne(cp => cp.Campaign)
                    .WithMany(c => c.CampaignProducts)
                    .HasForeignKey(cp => cp.CampaignId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(cp => cp.Product)
                    .WithMany()
                    .HasForeignKey(cp => cp.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(cp => cp.Listing)
                    .WithMany()
                    .HasForeignKey(cp => cp.ListingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // CampaignCategory
            modelBuilder.Entity<CampaignCategory>(b =>
            {
                b.HasOne(cc => cc.Campaign)
                    .WithMany(c => c.CampaignCategories)
                    .HasForeignKey(cc => cc.CampaignId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(cc => cc.Category)
                    .WithMany()
                    .HasForeignKey(cc => cc.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // CampaignUsage
            modelBuilder.Entity<CampaignUsage>(b =>
            {
                b.HasOne(cu => cu.Campaign)
                    .WithMany(c => c.Usages)
                    .HasForeignKey(cu => cu.CampaignId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(cu => cu.User)
                    .WithMany()
                    .HasForeignKey(cu => cu.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(cu => cu.Order)
                    .WithMany()
                    .HasForeignKey(cu => cu.OrderId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.Property(cu => cu.DiscountApplied).HasColumnType("decimal(18,2)");
            });

            // BANNER
            modelBuilder.Entity<Banner>(b =>
            {
                b.Property(x => x.Position)
                    .HasConversion<string>()
                    .HasMaxLength(30);

                b.HasIndex(x => new { x.Position, x.DisplayOrder });
            });

            // SITE SETTINGS
            modelBuilder.Entity<SiteSetting>(b =>
            {
                b.HasIndex(x => x.Key)
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");

                b.Property(x => x.Category)
                    .HasConversion<string>()
                    .HasMaxLength(30);
            });

            // SUPPORT TICKET
            modelBuilder.Entity<SupportTicket>(b =>
            {
                b.HasIndex(x => x.TicketNumber)
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");

                b.HasIndex(x => new { x.Status, x.Priority, x.CreatedAt });

                b.Property(x => x.Category)
                    .HasConversion<string>()
                    .HasMaxLength(30);

                b.Property(x => x.Priority)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                b.Property(x => x.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30);

                b.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.Order)
                    .WithMany()
                    .HasForeignKey(x => x.OrderId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne(x => x.SellerOrder)
                    .WithMany()
                    .HasForeignKey(x => x.SellerOrderId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne(x => x.AssignedTo)
                    .WithMany()
                    .HasForeignKey(x => x.AssignedToId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // SUPPORT TICKET MESSAGE
            modelBuilder.Entity<SupportTicketMessage>(b =>
            {
                b.HasOne(x => x.Ticket)
                    .WithMany(t => t.Messages)
                    .HasForeignKey(x => x.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Sender)
                    .WithMany()
                    .HasForeignKey(x => x.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(x => new { x.TicketId, x.CreatedAt });
            });

            // SELLER PROFILE
            modelBuilder.Entity<SellerProfile>(b =>
            {
                b.HasOne(x => x.User)
                    .WithOne()
                    .HasForeignKey<SellerProfile>(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(x => x.UserId)
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");

                b.HasIndex(x => x.VerificationStatus);

                b.Property(x => x.VerificationStatus)
                    .HasConversion<string>()
                    .HasMaxLength(20);
            });

        }

        public override int SaveChanges()
        {
            var now = DateTime.UtcNow;
            TouchCartsIfCartItemsChanged(now);
            ApplyAuditAndSoftDelete(now);
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            TouchCartsIfCartItemsChanged(now);
            ApplyAuditAndSoftDelete(now);
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditAndSoftDelete(DateTime now)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property(x => x.CreatedAt).IsModified = false;
                    entry.Entity.UpdatedAt = now;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    // Hard delete'i soft delete'e çevir
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedDate ??= now;
                    entry.Entity.UpdatedAt = now;

                    entry.Property(x => x.CreatedAt).IsModified = false;
                }
            }
        }

        private void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                    continue;

                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var isDeletedProp = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var body = Expression.Equal(isDeletedProp, Expression.Constant(false));
                var lambda = Expression.Lambda(body, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        private void TouchCartsIfCartItemsChanged(DateTime now)
        {
            var cartIds = ChangeTracker.Entries<CartItem>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .Select(e => e.Entity.CartId)
                .Distinct()
                .ToList();

            foreach (var cartId in cartIds)
            {
                // Eğer aynı Cart zaten track ediliyorsa, tekrar Attach etme!
                var trackedCartEntry = ChangeTracker.Entries<Cart>()
                    .FirstOrDefault(e => e.Entity.Id == cartId);

                if (trackedCartEntry != null)
                {
                    trackedCartEntry.Entity.UpdatedAt = now;
                    trackedCartEntry.Property(x => x.UpdatedAt).IsModified = true;
                    trackedCartEntry.Property(x => x.CreatedAt).IsModified = false;
                    continue;
                }

                // Track edilmiyorsa stub attach et
                var cart = new Cart { Id = cartId };
                Carts.Attach(cart);

                Entry(cart).Property(x => x.UpdatedAt).CurrentValue = now;
                Entry(cart).Property(x => x.UpdatedAt).IsModified = true;
                Entry(cart).Property(x => x.CreatedAt).IsModified = false;
            }
        }

    } 
}

# Exodus

Full-featured multi-vendor marketplace platform built with ASP.NET Core 10, Entity Framework Core, and SQL Server. Supports multiple sellers, advanced order management, payment processing with iyzico, loyalty programs, and a comprehensive admin panel.

## Features

### Authentication & Security
- JWT-based authentication with refresh token support
- Two-factor authentication (2FA)
- Email verification and password reset
- Role-based access control (Admin, Seller, Customer)
- Input sanitization and global exception handling
- Audit logging for all critical actions

### Product & Catalog
- Product management with categories, brands, and attributes
- Hierarchical category tree (parent/child)
- Product images with primary image support
- Barcode management
- Product Q&A (questions and answers with upvoting)
- Product comparison and recently viewed tracking

### Marketplace & Listings
- Multi-seller support - multiple sellers can list the same product
- Listing conditions (New, Like New, Used, Refurbished)
- Per-seller pricing and stock management
- Low stock alerts and out-of-stock tracking
- Seller profiles and seller reviews/ratings

### Shopping & Orders
- Shopping cart with real-time stock validation
- Checkout with automatic order splitting per seller
- Order tracking with status timeline
- Order cancellation and refund requests
- Multi-seller order management (SellerOrder per vendor)
- Invoice generation

### Payment Processing
- iyzico payment gateway integration
- 3D Secure payment support
- Payment authorization, capture, and cancellation
- BIN checking and installment options
- Multiple payment methods (Credit Card, Debit Card, Bank Transfer, Cash on Delivery, Wallet, Installment, Buy Now Pay Later)
- Payment simulation endpoints for testing

### Shipping & Logistics
- Shipment creation and tracking with carrier info
- Shipment status timeline (Created, Packed, Shipped, Delivered)
- Return shipment management
- Seller-specific shipping settings
- Region and shipping zone management

### Loyalty Program
- Earn points on every purchase
- Tier-based system (e.g., Bronze, Silver, Gold)
- Spend points as discount on orders
- Point estimation before checkout
- Full transaction history (earnings/spendings)

### Campaigns & Promotions
- Campaign management (percentage discount, fixed amount, coupons)
- Campaign scoping (all products, specific categories, specific products)
- Campaign usage tracking
- Digital gift cards (purchase, gift, redeem)
- Seller-specific campaigns

### Customer Engagement
- Wishlist management
- Product comparison
- Recently viewed products
- Product reviews and ratings
- Seller reviews and ratings
- Notification system (in-app notifications, read/unread tracking)
- Notification preferences per user

### Customer Support
- Support ticket creation with file attachments
- Ticket messaging (customer-agent conversation)
- Ticket status management (open, closed, reopened)
- Customer satisfaction rating on closure

### Admin Panel
- **Dashboard**: Sales statistics, order/user/product stats, recent activities
- **Reports**: Sales reports, inventory reports, customer reports
- **User Management**: Full CRUD for all users
- **Seller Management**: Seller approval, payout management
- **Product Management**: Product moderation, image management
- **Order Management**: Order overview, status updates, refund processing
- **Payment Administration**: Payment monitoring and management
- **Category & Brand Management**: Full CRUD with hierarchy
- **Campaign Management**: Create and manage promotions
- **Gift Card Management**: Issue and manage gift cards
- **Tax & Region Management**: Tax rates by region
- **Content Management**: Static pages (About, Contact, etc.)
- **Banner & Widgets**: Homepage banner and widget management
- **Review Moderation**: Product and seller review moderation
- **Product Q&A Moderation**: Question/answer moderation
- **Notification Management**: Send notifications to users
- **Email Templates**: Manage email templates
- **Site Settings**: Commission rates, system configuration
- **Affiliate Program**: Affiliate management
- **Audit Logs**: View system audit trail

## Tech Stack

- **Framework**: ASP.NET Core 10.0
- **Database**: SQL Server with Entity Framework Core 10.0
- **Authentication**: JWT Bearer Token with Refresh Tokens
- **Payment Gateway**: iyzico (3D Secure support)
- **Validation**: FluentValidation 11.3.1
- **API Documentation**: Swagger/Swashbuckle with JWT auth support
- **Password Hashing**: BCrypt.Net
- **File Handling**: Product images, avatars, ticket attachments

## Architecture

- Clean/Layered Architecture (Controllers → Services → DbContext)
- Repository Pattern via Services
- Soft Delete Pattern (IsDeleted flag, data never truly deleted)
- Transaction Support with Retry Strategies
- Global Exception Handling Middleware
- Input Sanitization Service
- Multi-tenant seller isolation

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- SQL Server (LocalDB, Express, or Full)
- Visual Studio 2022 / VS Code / Rider

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd Exodus
   ```

2. **Configure Database Connection**

   Update the connection string in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER;Database=Exodusdb;..."
   }
   ```

3. **Configure JWT Settings**

   Update JWT settings in `appsettings.json`:
   ```json
   "JwtSettings": {
     "SecretKey": "YourSuperSecretKey_MinimumLength32Characters!",
     "Issuer": "Exodus",
     "Audience": "ExodusUsers",
     "ExpiryMinutes": 1440
   }
   ```

4. **Apply Database Migrations**
   ```bash
   dotnet ef database update
   ```

5. **Run the Application**
   ```bash
   dotnet run
   ```

6. **Access Swagger UI**

   Navigate to: `http://localhost:5013/swagger`

### Environment Variables (Production)

For production deployment, use environment variables instead of hardcoded values:

```bash
cp .env.example .env
```

Required environment variables:
- `ConnectionStrings__DefaultConnection`: Database connection string
- `JwtSettings__SecretKey`: JWT secret key (min 32 characters)
- `Cors__AllowedOrigins__0`: Allowed frontend origins

## API Endpoints

### Authentication
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/auth/register` | Register new user | - |
| POST | `/api/auth/login` | Login with credentials | - |
| POST | `/api/auth/login/2fa` | Login with 2FA code | - |
| POST | `/api/auth/verify-email` | Verify email address | - |
| POST | `/api/auth/forgot-password` | Request password reset | - |
| POST | `/api/auth/reset-password` | Reset password | - |
| POST | `/api/auth/refresh-token` | Refresh JWT token | - |
| GET | `/api/auth/me` | Get current user info | Yes |

### Profile
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/profile` | Get user profile | Yes |
| PUT | `/api/profile` | Update profile | Yes |
| PUT | `/api/profile/password` | Change password | Yes |
| POST | `/api/profile/avatar` | Upload avatar | Yes |
| DELETE | `/api/profile/avatar` | Delete avatar | Yes |
| GET | `/api/profile/addresses` | Get addresses | Yes |
| POST | `/api/profile/addresses` | Create address | Yes |

### Products
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/products` | List all products | - |
| GET | `/api/products/{id}` | Get product by ID | - |
| POST | `/api/products` | Create product | Seller/Admin |
| PUT | `/api/products/{id}` | Update product | Seller/Admin |
| DELETE | `/api/products/{id}` | Delete product | Seller/Admin |

### Categories
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/categories` | List all categories | - |
| GET | `/api/categories/{id}` | Get category by ID | - |
| GET | `/api/categories/tree` | Get category tree | - |
| GET | `/api/categories/slug/{slug}` | Get by slug | - |

### Listings
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/listings` | List all listings | - |
| GET | `/api/listings/{id}` | Get listing by ID | - |
| POST | `/api/listings` | Create listing | Seller/Admin |
| PUT | `/api/listings/{id}` | Update listing | Seller/Admin |
| DELETE | `/api/listings/{id}` | Delete listing | Seller/Admin |

### Cart
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/cart` | Get my cart | Yes |
| POST | `/api/cart/add` | Add item to cart | Yes |
| PUT | `/api/cart/item/{id}` | Update item quantity | Yes |
| DELETE | `/api/cart/item/{id}` | Remove item | Yes |

### Orders
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/orders/checkout` | Create order from cart | Yes |
| GET | `/api/orders/{id}` | Get order by ID | Yes |
| GET | `/api/orders/my` | Get my orders | Yes |
| POST | `/api/orders/{id}/cancel` | Cancel order | Yes |
| POST | `/api/orders/{id}/refund` | Request refund | Yes |

### Payments
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/payments/intents` | Create payment intent | Yes |
| GET | `/api/payments/order/{id}` | Get payment by order | Yes |
| POST | `/api/payments/{id}/authorize` | Authorize payment | Yes |
| POST | `/api/payments/{id}/capture` | Capture payment | Yes |
| POST | `/api/payments/{id}/cancel` | Cancel payment | Yes |
| POST | `/api/payments/{id}/refund` | Refund payment | Yes |
| POST | `/api/payments/{id}/3dsecure` | 3D Secure confirm | Yes |
| GET | `/api/payments/bin/{bin}` | BIN check | Yes |
| GET | `/api/payments/installments` | Get installment options | Yes |

### Seller
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/seller/listings` | Get my listings | Seller |
| POST | `/api/seller/listings` | Create listing | Seller |
| PUT | `/api/seller/listings/{id}` | Update listing | Seller |
| PATCH | `/api/seller/listings/{id}/stock` | Update stock | Seller |
| GET | `/api/seller/listings/low-stock` | Low stock items | Seller |
| GET | `/api/seller/orders` | Get my orders | Seller |
| POST | `/api/seller/orders/{id}/confirm` | Confirm order | Seller |
| POST | `/api/seller/orders/{id}/ship` | Ship order | Seller |

### Shipments
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/shipments/seller-orders/{id}` | Get shipment | Seller/Admin |
| PATCH | `/api/shipments/seller-orders/{id}/ship` | Mark shipped | Seller/Admin |
| PATCH | `/api/shipments/seller-orders/{id}/deliver` | Mark delivered | Seller/Admin |
| GET | `/api/shipments/seller-orders/{id}/timeline` | Get timeline | Seller/Admin |

### Loyalty
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/loyalty/points` | Get my points & tier | Yes |
| GET | `/api/loyalty/history` | Transaction history | Yes |
| POST | `/api/loyalty/spend` | Spend points | Yes |
| GET | `/api/loyalty/estimate` | Estimate earnable points | Yes |
| GET | `/api/loyalty/value` | Calculate point value | Yes |

### Campaigns
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/campaigns` | List active campaigns | - |
| GET | `/api/campaigns/{id}` | Get campaign details | - |

### Support
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/support/tickets` | Create ticket | Yes |
| GET | `/api/support/tickets` | Get my tickets | Yes |
| GET | `/api/support/tickets/{id}` | Get ticket details | Yes |
| POST | `/api/support/tickets/{id}/reply` | Reply to ticket | Yes |
| POST | `/api/support/tickets/{id}/close` | Close ticket | Yes |
| POST | `/api/support/tickets/{id}/reopen` | Reopen ticket | Yes |

### Notifications
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/notifications` | Get notifications | Yes |
| GET | `/api/notifications/unread-count` | Unread count | Yes |
| PATCH | `/api/notifications/{id}/read` | Mark as read | Yes |
| PATCH | `/api/notifications/read-all` | Mark all as read | Yes |

### Wishlist & Engagement
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/wishlist` | Get wishlist | Yes |
| POST | `/api/wishlist` | Add to wishlist | Yes |
| DELETE | `/api/wishlist/{id}` | Remove from wishlist | Yes |
| GET | `/api/comparison` | Get comparisons | Yes |
| POST | `/api/comparison` | Add to comparison | Yes |
| GET | `/api/recently-viewed` | Recently viewed | Yes |

### Product Q&A
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/products/{id}/questions` | Get questions | - |
| POST | `/api/products/{id}/questions` | Ask question | Yes |
| POST | `/api/products/questions/{id}/answer` | Answer question | Seller/Admin |
| POST | `/api/products/questions/{id}/upvote` | Upvote question | Yes |

### Admin Endpoints
All admin endpoints are under `/api/admin/*` and require Admin role.

| Area | Prefix | Description |
|------|--------|-------------|
| Dashboard | `/api/admin/dashboard` | Statistics, reports, recent activities |
| Users | `/api/admin/users` | User CRUD and management |
| Sellers | `/api/admin/sellers` | Seller approval and management |
| Products | `/api/admin/products` | Product moderation, images |
| Orders | `/api/admin/orders` | Order management, refund processing |
| Categories | `/api/admin/categories` | Category CRUD with hierarchy |
| Brands | `/api/admin/brands` | Brand management |
| Campaigns | `/api/admin/campaigns` | Campaign CRUD |
| Gift Cards | `/api/admin/giftcards` | Gift card management |
| Payments | `/api/admin/payments` | Payment administration |
| Shipments | `/api/admin/shipments` | Shipment administration |
| Tax Rates | `/api/admin/tax` | Tax rate management |
| Regions | `/api/admin/regions` | Shipping zone management |
| Banners | `/api/admin/banners` | Homepage banners |
| Widgets | `/api/admin/home-widgets` | Homepage widgets |
| Reviews | `/api/admin/reviews` | Review moderation |
| Q&A | `/api/admin/product-qa` | Q&A moderation |
| Notifications | `/api/admin/notifications` | Send notifications |
| Support | `/api/admin/support` | Ticket management |
| Reports | `/api/admin/reports` | Sales, inventory, customer reports |
| Settings | `/api/admin/settings` | Site configuration |
| Content | `/api/admin/content` | Static pages (About, Contact) |
| Email Templates | `/api/admin/email-templates` | Email template management |
| Affiliates | `/api/admin/affiliates` | Affiliate program |
| Audit Logs | `/api/admin/audit` | System audit trail |
| Seller Payouts | `/api/admin/seller-payouts` | Seller payout management |
| Listings | `/api/admin/listings` | Listing administration |
| Seller Reviews | `/api/admin/seller-reviews` | Seller review moderation |

## User Roles

| Role | Value | Description |
|------|-------|-------------|
| Customer | 0 | Default role. Can browse, purchase, review, and manage orders |
| Seller | 1 | Can create listings, manage stock, process orders, and track payouts |
| Admin | 2 | Full access to all endpoints including admin panel |

## Database Schema

### Core Entities (56 tables)

**Users & Auth**: Users, RefreshToken, NotificationPreferences

**Products & Catalog**: Product, ProductImage, ProductAttribute, ProductBarcodes, Category, Brand, Listing

**Orders & Transactions**: Order, SellerOrder, SellerOrderItem, OrderEvent, Cart, CartItem, Invoice, Refund

**Payments**: PaymentIntent, PaymentEvent

**Shipping**: Shipment, ShipmentEvent, ReturnShipment, SellerShippingSettings, ShippingCarrier, Region, TaxRate

**Loyalty & Engagement**: LoyaltyPoint, Wishlist, ProductComparison, RecentlyViewed, Review, SellerReview, SellerProfile

**Campaigns & Promotions**: Campaign, CampaignProduct, CampaignUsage, GiftCard, GiftCardUsage

**Content & Support**: Banner, HomeWidget, StaticPage, SupportTicket, Notification, EmailTemplate

**System**: SiteSetting, AuditLog, Affiliate

**Q&A**: ProductQuestion

## Development

### Creating Migrations

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Testing with Swagger

1. Run the application: `dotnet run`
2. Open Swagger UI: `http://localhost:5013/swagger`
3. Register a user via `POST /api/auth/register`
4. Login via `POST /api/auth/login` to get a JWT token
5. Click "Authorize" button in Swagger and paste the token
6. Test endpoints with authentication

### Test Flow Example

```
Register (Seller) → Login → Get Token → Authorize in Swagger
→ Create Category (Admin) → Create Product → Create Listing
→ Register (Buyer) → Login → Add to Cart → Checkout
→ Create Payment → Seller Ships → Delivered
```

## License

MIT License

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

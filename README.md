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
- Admin-only payment simulation endpoints for testing (Development environment only)

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
- Wishlist data model with admin-side wishlist administration
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
- SQL Server (Docker, LocalDB, Express, or Full)
- Visual Studio 2022 / VS Code / Rider

### Local setup (macOS/Linux)

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd Exodus
   ```

2. **Start SQL Server in Docker**

   On Apple Silicon the image runs under emulation, hence `--platform linux/amd64`. Replace the placeholder with your own strong password and keep it out of the repository.
   ```bash
   docker run --platform linux/amd64 --name exodus-sql \
     -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<YourStrong!Passw0rd>" \
     -p 127.0.0.1:1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
   ```

3. **Store the connection string in user-secrets** (the project already has a `UserSecretsId`, so secrets stay out of `appsettings.json`)
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
     "Server=127.0.0.1,1433;Database=Exodusdb;User Id=sa;Password=<YourStrong!Passw0rd>;TrustServerCertificate=True"
   ```

4. **Run the application**
   ```bash
   dotnet run --launch-profile http
   ```
   On startup the app applies the EF Core migrations automatically (a single `Baseline` migration) and, in every non-Production environment, seeds demo data (see [Demo accounts](#demo-accounts)). If you ran an older build of this project, drop the old `Exodusdb` database first so the baseline migration and seed data start clean.

5. **Open Swagger UI**: `http://localhost:5013/swagger`

The `JwtSettings:SecretKey` in `appsettings.json` is a development-only placeholder. Run `dotnet test Exodus.sln` to execute the test suite (it uses an in-memory database, no SQL Server needed).

### Environment Variables (Production)

In Production no demo data is seeded and the placeholder JWT key is not accepted: startup validation requires a real secret. Provide configuration through environment variables (see `.env.example`):

- `ConnectionStrings__DefaultConnection`: Database connection string
- `JwtSettings__SecretKey`: JWT secret key (min 32 characters, required)
- `Cors__AllowedOrigins__0`: Allowed frontend origins

## Demo accounts

Seeded automatically outside Production. All accounts use the password `Demo123!`.

| Role | Email |
|------|-------|
| Seller | `zeynep@test.com` |
| Seller | `mehmet@test.com` |
| Customer | `ahmet@test.com` (default address included) |
| Customer | `elif@test.com` (default address included) |
| Customer | `can@test.com` (default address included) |

The seed also creates 5 categories, 18 products, competing listings from both sellers (including low-stock and out-of-stock examples) and 5 demo orders. There is no seeded admin: register one via `POST /api/auth/register` with `"role": 2` (see `api-test.http`).

## API Endpoints

Routes follow the controller names (singular), e.g. `/api/product`, `/api/order`, `/api/payment`. Swagger is the authoritative reference.

### Authentication
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/auth/register` | Register new user | - |
| POST | `/api/auth/login` | Login with credentials | - |
| POST | `/api/auth/login/2fa` | Login with 2FA code | - |
| GET | `/api/auth/verify-email?token=` | Verify email address | - |
| POST | `/api/auth/resend-verification` | Resend verification email | - |
| POST | `/api/auth/forgot-password` | Request password reset | - |
| POST | `/api/auth/reset-password` | Reset password | - |
| POST | `/api/auth/refresh` | Refresh JWT token | - |
| POST | `/api/auth/revoke` | Revoke refresh token | Yes |
| GET | `/api/auth/me` | Get current user info | Yes |

Two-factor setup lives under `/api/twofactor` (`setup`, `verify`, `disable`, `status`, `backup-codes/regenerate`).

### Profile & Addresses
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/profile` | Get user profile | Yes |
| PUT | `/api/profile` | Update profile | Yes |
| POST | `/api/profile/change-password` | Change password | Yes |
| POST | `/api/profile/avatar` | Upload avatar | Yes |
| DELETE | `/api/profile/avatar` | Delete avatar | Yes |
| GET | `/api/profile/addresses` | Get addresses | Yes |
| POST | `/api/profile/addresses` | Create address | Yes |
| PATCH | `/api/profile/addresses/{id}/default` | Set default address | Yes |

A standalone address API is also available under `/api/address`.

### Products
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/product` | List / search products | - |
| GET | `/api/product/{id}` | Get product by ID | - |
| POST | `/api/product` | Create product | Seller/Admin |
| PUT | `/api/product/{id}` | Update product | Seller/Admin |
| DELETE | `/api/product/{id}` | Delete product | Seller/Admin |

### Categories
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/category` | List all categories | - |
| GET | `/api/category/{id}` | Get category by ID | - |
| GET | `/api/category/{id}/subcategories` | Get subcategories | - |
| GET | `/api/category/tree` | Get category tree | - |
| GET | `/api/category/slug/{slug}` | Get by slug | - |

### Listings
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/listings` | List all listings | - |
| GET | `/api/listings/search` | Search listings (filter, sort, paginate) | - |
| GET | `/api/listings/{id}` | Get listing by ID | - |
| POST | `/api/listings` | Create listing | Seller/Admin |
| PUT | `/api/listings/{id}` | Update listing | Seller/Admin |
| DELETE | `/api/listings/{id}` | Delete listing | Seller/Admin |

`GET /api/listings/search` query parameters: `search`, `categoryId`, `productId`, `minPrice`, `maxPrice`, `condition`, `sellerId`, `inStock`, `stockStatus`, `sortBy` (`price` | `createdAt` | `name` | `stock` | `seller`, default `price`), `sortOrder` (`asc` | `desc`, default `asc`), `page`, `pageSize` (clamped to 1..100). Response: `{ items, totalCount, page, pageSize, totalPages }`.

### Cart
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/cart/my` | Get my cart | Yes |
| POST | `/api/cart/add` | Add item to cart | Yes |
| PUT | `/api/cart/{userId}/item/{cartItemId}` | Update item quantity | Yes |
| DELETE | `/api/cart/{userId}/item/{cartItemId}` | Remove item | Yes |

### Orders
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/order/checkout` | Create order from cart | Yes |
| GET | `/api/order` | Get my orders | Yes |
| GET | `/api/order/{id}` | Get order by ID | Yes |
| GET | `/api/order/number/{orderNumber}` | Get order by number | Yes |
| POST | `/api/order/{id}/cancel` | Cancel order | Yes |
| POST | `/api/order/{id}/refund` | Request refund | Yes |

### Payments
Customer-facing endpoints verify that the caller owns the order (Admin bypasses). State transitions are Admin-only; the simulators exist only in the Development environment (404 elsewhere).

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/payment/intents` | Create payment intent | Owner/Admin |
| GET | `/api/payment/intents/{id}` | Get payment intent | Owner/Admin |
| GET | `/api/payment/order/{orderId}` | Get payment by order | Owner/Admin |
| GET | `/api/payment/intents/{id}/events` | Payment event history | Owner/Admin |
| POST | `/api/payment/intents/{id}/cancel` | Cancel payment | Owner/Admin |
| POST | `/api/payment/intents/{id}/authorize` | Authorize payment | Admin |
| POST | `/api/payment/intents/{id}/capture` | Capture payment | Admin |
| POST | `/api/payment/intents/{id}/refund` | Refund payment | Admin |
| POST | `/api/payment/intents/{id}/mark-received` | Mark payment received (bank transfer / COD) | Admin |
| POST | `/api/payment/intents/{id}/simulate-success` | Simulate success (Development only) | Admin |
| POST | `/api/payment/intents/{id}/simulate-fail` | Simulate failure (Development only) | Admin |
| POST | `/api/payment/intents/{id}/3ds/confirm` | 3DS simulator (Development only) | Admin |
| POST | `/api/payment/gateway/process` | Process card payment via iyzico | Yes |
| POST | `/api/payment/gateway/3ds/initialize` | Start 3D Secure payment | Yes |
| POST | `/api/payment/gateway/3ds/complete` | 3D Secure callback (real 3DS completion) | - |
| POST | `/api/payment/3ds-callback` | 3D Secure browser callback from iyzico | - |
| GET | `/api/payment/gateway/bin/{binNumber}` | BIN check | Yes |
| GET | `/api/payment/gateway/installments` | Get installment options | Yes |
| POST | `/api/payment/webhook/{provider}` | Provider webhook | - |

### Seller
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/seller/listings` | Get my listings | Seller |
| POST | `/api/seller/listings` | Create listing | Seller |
| PUT | `/api/seller/listings/{id}` | Update listing | Seller |
| DELETE | `/api/seller/listings/{id}` | Delete listing | Seller |
| PATCH | `/api/seller/listings/{id}/stock` | Update stock | Seller |
| GET | `/api/seller/listings/low-stock` | Low stock items | Seller |
| GET | `/api/seller/listings/out-of-stock` | Out-of-stock items | Seller |
| GET | `/api/seller/orders` | Get my seller orders | Seller |
| POST | `/api/seller/orders/{id}/confirm` | Confirm order | Seller |
| POST | `/api/seller/orders/{id}/ship` | Ship order (carrier + trackingNumber required, creates the Shipment) | Seller |
| PATCH | `/api/seller/orders/{id}/status` | Update status (Shipped/Delivered/Completed rejected; use ship / deliver) | Seller |

Seller campaigns are under `/api/seller/campaigns`.

### Shipments
Seller ownership is enforced on every call (Admin bypasses).

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
| GET | `/api/loyalty/transactions` | Transaction history | Yes |
| POST | `/api/loyalty/spend` | Spend points | Yes |
| GET | `/api/loyalty/estimate-earn` | Estimate earnable points | Yes |
| GET | `/api/loyalty/calculate-value` | Calculate point value | Yes |

### Campaigns
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/campaign/active` | List active campaigns | - |
| GET | `/api/campaign/applicable` | Campaigns applicable to a cart/product | Yes |
| POST | `/api/campaign/calculate` | Calculate discounts | Yes |
| GET | `/api/campaign/validate-coupon` | Validate coupon code | Yes |

### Support
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/support/tickets` | Create ticket | Yes |
| GET | `/api/support/tickets` | Get my tickets | Yes |
| GET | `/api/support/tickets/{id}` | Get ticket details | Yes |
| POST | `/api/support/tickets/{id}/messages` | Reply to ticket | Yes |
| POST | `/api/support/tickets/{id}/close` | Close ticket | Yes |
| POST | `/api/support/tickets/{id}/reopen` | Reopen ticket | Yes |

### Notifications
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/notification` | Get notifications | Yes |
| GET | `/api/notification/unread-count` | Unread count | Yes |
| PATCH | `/api/notification/{id}/read` | Mark as read | Yes |
| PATCH | `/api/notification/read-all` | Mark all as read | Yes |
| DELETE | `/api/notification/{id}` | Delete notification | Yes |

### Comparison & Recently Viewed
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/comparison` | Get comparisons | Yes |
| POST | `/api/comparison` | Create comparison | Yes |
| POST | `/api/comparison/{id}/products/{productId}` | Add product to comparison | Yes |
| GET | `/api/recentlyviewed` | Recently viewed | Yes |
| POST | `/api/recentlyviewed/{productId}` | Record a product view | Yes |

### Reviews & Product Q&A
| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/sellers/{sellerId}/reviews` | List seller reviews | - |
| GET | `/api/sellers/{sellerId}/rating-summary` | Seller rating summary | - |
| POST | `/api/sellers/{sellerId}/reviews` | Review a seller (after a purchase) | Yes |
| GET | `/api/products/{productId}/questions` | Get questions | - |
| POST | `/api/products/{productId}/questions` | Ask question | Yes |
| POST | `/api/products/{productId}/questions/{questionId}/answers` | Answer question | Yes |
| POST | `/api/products/{productId}/questions/{questionId}/upvote` | Upvote question | Yes |

### Admin Endpoints
All admin endpoints are under `/api/admin/*` and require the Admin role.

| Area | Prefix | Description |
|------|--------|-------------|
| Dashboard | `/api/admin/dashboard` | Statistics, recent activities |
| Users | `/api/admin/users` | User CRUD and management |
| Sellers | `/api/admin/sellers` | Seller approval and management |
| Products | `/api/admin/products` | Product moderation, images |
| Attributes | `/api/admin/attributes` | Product attributes |
| Orders | `/api/admin/orders` | Order management, refund processing |
| Categories | `/api/admin/categories` | Category CRUD with hierarchy |
| Brands | `/api/admin/brands` | Brand management |
| Campaigns | `/api/admin/campaigns` | Campaign CRUD |
| Gift Cards | `/api/admin/gift-cards` | Gift card management |
| Payments | `/api/admin/payments` | Payment administration |
| Shipments | `/api/admin/shipments` | Shipment administration |
| Return Shipments | `/api/admin/return-shipments` | Return shipment management |
| Invoices | `/api/admin/invoices` | Invoice administration |
| Taxes | `/api/admin/taxes` | Tax rate management |
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
| Loyalty | `/api/admin/loyalty` | Loyalty administration |
| Wishlists | `/api/admin/wishlists` | Wishlist administration |
| Listings | `/api/admin/listings` | Listing administration |
| Seller Reviews | `/api/admin/seller-reviews` | Seller review moderation |

## Security fixes

Behavior hardened on this branch (each covered by integration tests):

- **Payment ownership**: every customer-facing payment endpoint checks that the caller owns the order (Admin bypasses). `authorize`, `capture`, `refund`, `mark-received`, `simulate-success`, `simulate-fail` and `3ds/confirm` are Admin-only, and the simulators plus `3ds/confirm` only exist in Development (404 elsewhere). Real 3DS completes through the anonymous `gateway/3ds/complete` callback.
- **Seller ownership in shipping**: `/api/shipments/seller-orders/{id}...` enforces that the seller owns the seller order (Admin bypasses). The legacy `SellerController` (`/api/seller/{sellerId}/orders`, `PATCH /api/seller/orders/{id}/ship|deliver`) was removed. Shipping goes through `POST /api/seller/orders/{id}/ship` (carrier + trackingNumber required) and delivery through `PATCH /api/shipments/seller-orders/{id}/deliver`.
- **Order status integrity**: `PATCH /api/seller/orders/{id}/status` rejects Shipped/Delivered/Completed; Cancelled and Delivered seller orders are terminal; the parent order status is derived from its seller orders forward-only (a paid order never regresses to Pending; cancelled, failed, refunded and completed orders are left untouched).
- **Production safety**: demo data is never seeded in Production, and a real `JwtSettings__SecretKey` is required there.

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

The migration history is a single `Baseline` migration; migrations are applied automatically at startup.

```bash
dotnet ef migrations add MigrationName
dotnet ef database update   # optional, the app also migrates on startup
```

### Testing with Swagger

1. Run the application: `dotnet run --launch-profile http`
2. Open Swagger UI: `http://localhost:5013/swagger`
3. Log in as a [demo account](#demo-accounts) (or register a user via `POST /api/auth/register`)
4. Login via `POST /api/auth/login` to get a JWT token
5. Click "Authorize" button in Swagger and paste the token
6. Test endpoints with authentication

### Test Flow Example

```
Register (Seller) → Login → Get Token → Authorize in Swagger
→ Create Category (Admin) → Create Product → Create Listing
→ Register (Buyer) → Login → Add to Cart → Checkout
→ Create Payment → (Admin) Simulate Success [Development] → Seller Ships (carrier + tracking) → Delivered
```

## License

MIT License

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

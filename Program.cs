using Exodus.Data;
using Exodus.Models;
using Exodus.Services.Auth;
using Exodus.Services.Carts;
using Exodus.Services.Common;
using Exodus.Services.Email;
using Exodus.Services.Listings;
using Exodus.Services.Products;
using Exodus.Services.Users;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using Exodus.Services.Orders;
using Exodus.Services.Payments;
using Exodus.Services.Shipments;
using Exodus.Services.Audit;
using Exodus.Services.Security;
using Exodus.Services.TwoFactor;
using Exodus.Services.Categories;
using Exodus.Services.Files;
using Exodus.Services.Profile;
using Exodus.Services.Notifications;
using Exodus.Services.Dashboard;
using Exodus.Services.Reports;
using Exodus.Services.PaymentGateway;
using Exodus.Services.Campaigns;
using Exodus.Services.Addresses;
using Exodus.Services.ProductQA;
using Exodus.Services.RecentlyViewedProducts;
using Exodus.Services.SellerReviews;
using Exodus.Services.Loyalty;
using Exodus.Services.Comparison;
using Exodus.Models.Dto;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// SERVICES (Build'den ÖNCE)
// --------------------

// Controllers + Enum String Converter
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();


// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Settings Configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Email Settings Configuration
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (jwtSettings == null)
    throw new InvalidOperationException("JWT settings are not configured properly");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("SellerOnly", policy => policy.RequireRole("Seller", "Admin"));
    options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer", "Seller", "Admin"));
});

// CORS Configuration
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global rate limit: 100 requests per minute per IP
    options.AddPolicy("fixed", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Auth endpoints: 10 requests per minute per IP (brute force protection)
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Sensitive operations: 5 requests per minute
    options.AddPolicy("sensitive", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// HttpContextAccessor for AuditService
builder.Services.AddHttpContextAccessor();

// DI - Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IListingService, ListingService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IShipmentService, ShipmentService>();

// Security Services
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IInputSanitizerService, InputSanitizerService>();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();

// Category & File Services
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.Configure<FileUploadSettings>(builder.Configuration.GetSection("FileUploadSettings"));
builder.Services.AddScoped<IFileService, FileService>();

// Profile & Notification Services
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Dashboard & Report Services
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Payment Gateway (iyzico)
builder.Services.Configure<IyzicoSettings>(builder.Configuration.GetSection("IyzicoSettings"));
builder.Services.AddHttpClient<IPaymentGateway, IyzicoPaymentGateway>();

// Campaign Service
builder.Services.AddScoped<ICampaignService, CampaignService>();

// Address Service
builder.Services.AddScoped<IAddressService, AddressService>();

// Product Q&A Service
builder.Services.AddScoped<IProductQAService, ProductQAService>();

// Recently Viewed Service
builder.Services.AddScoped<IRecentlyViewedService, RecentlyViewedService>();

// Seller Review Service
builder.Services.AddScoped<ISellerReviewService, SellerReviewService>();

// Loyalty Service
builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();

// Product Comparison Service
builder.Services.AddScoped<IProductComparisonService, ProductComparisonService>();

// --------------------
// BUILD
// --------------------
var app = builder.Build();

// --------------------
// PIPELINE
// --------------------

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();

// Static files for uploaded content
app.UseStaticFiles();

// CORS must be before Authentication/Authorization
app.UseCors("AllowFrontend");

// Rate Limiting
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

app.MapControllers();

// --------------------
// SEED (Build'den sonra, Run'dan önce)
// --------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // await DbSeeder.SeedAsync(db);
}

app.Run();

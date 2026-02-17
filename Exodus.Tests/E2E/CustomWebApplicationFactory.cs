using System.Threading.RateLimiting;
using Exodus.Data;
using Exodus.Services.Email;
using Exodus.Services.PaymentGateway;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Exodus.Tests.E2E;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Verifies a user's email directly in the DB (bypassing the email verification flow).
    /// Required for tests that call the login endpoint, since login requires EmailVerified = true.
    /// </summary>
    public async Task VerifyUserEmailAsync(int userId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FindAsync(userId);
        if (user != null)
        {
            user.EmailVerified = true;
            await db.SaveChangesAsync();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext-related registrations to avoid dual-provider conflict
            // (SqlServer + InMemory cannot coexist in the same service provider)
            var dbContextDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true)
                .ToList();

            foreach (var descriptor in dbContextDescriptors)
                services.Remove(descriptor);

            // Remove the DB-based health check (it fails with dual-provider)
            services.RemoveAll(typeof(IHealthCheck));

            // Add InMemory database (name must be generated OUTSIDE the lambda
            // so all DbContext instances share the same in-memory store)
            var dbName = "ExodusTestDb_" + Guid.NewGuid().ToString("N");
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            // Re-add a simple health check (without DB check)
            services.AddHealthChecks();

            // Disable rate limiting for tests by removing all rate limiter config
            // and re-adding with no-limit policies
            var rateLimiterDescriptors = services
                .Where(d => d.ServiceType.IsGenericType &&
                            d.ServiceType.GetGenericArguments()
                                .Any(a => a.FullName?.Contains("RateLimiterOptions") == true))
                .ToList();
            foreach (var d in rateLimiterDescriptors)
                services.Remove(d);

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = 429;
                options.AddPolicy("fixed", _ =>
                    RateLimitPartition.GetNoLimiter("test"));
                options.AddPolicy("auth", _ =>
                    RateLimitPartition.GetNoLimiter("test"));
                options.AddPolicy("sensitive", _ =>
                    RateLimitPartition.GetNoLimiter("test"));
            });

            // Replace email service with a no-op implementation
            services.RemoveAll(typeof(IEmailService));
            services.AddScoped<IEmailService, FakeEmailService>();

            // Replace payment gateway with a fake implementation
            services.RemoveAll(typeof(IPaymentGateway));
            services.AddScoped<IPaymentGateway, FakePaymentGateway>();
        });
    }
}

public class FakeEmailService : IEmailService
{
    public Task SendEmailVerificationAsync(string email, string token) => Task.CompletedTask;
    public Task SendPasswordResetAsync(string email, string token) => Task.CompletedTask;
    public Task SendAccountLockedAsync(string email, DateTime lockoutEndTime) => Task.CompletedTask;
}

public class FakePaymentGateway : IPaymentGateway
{
    public string ProviderName => "FakeGateway";

    public Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(new PaymentInitiationResult
        {
            Success = true,
            PaymentId = "fake-pay-" + Guid.NewGuid().ToString("N"),
            PaymentTransactionId = "fake-txn-" + Guid.NewGuid().ToString("N")
        });
    }

    public Task<PaymentResult> CompletePaymentAsync(string paymentToken, CancellationToken ct = default)
    {
        return Task.FromResult(new PaymentResult
        {
            Success = true,
            PaymentId = "fake-pay-" + Guid.NewGuid().ToString("N"),
            PaymentTransactionId = "fake-txn-" + Guid.NewGuid().ToString("N")
        });
    }

    public Task<Payment3DSResult> Initiate3DSPaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(new Payment3DSResult
        {
            Success = true,
            ThreeDSHtmlContent = "<html>3DS</html>",
            PaymentId = "fake-3ds-" + Guid.NewGuid().ToString("N")
        });
    }

    public Task<PaymentResult> Complete3DSPaymentAsync(string paymentToken, CancellationToken ct = default)
    {
        return Task.FromResult(new PaymentResult
        {
            Success = true,
            PaymentId = "fake-3ds-pay-" + Guid.NewGuid().ToString("N"),
            PaymentTransactionId = "fake-3ds-txn-" + Guid.NewGuid().ToString("N")
        });
    }

    public Task<RefundResult> RefundAsync(string paymentTransactionId, decimal? amount = null, CancellationToken ct = default)
    {
        return Task.FromResult(new RefundResult
        {
            Success = true,
            PaymentId = "fake-refund-" + Guid.NewGuid().ToString("N")
        });
    }

    public Task<PaymentStatusResult> GetPaymentStatusAsync(string paymentTransactionId, CancellationToken ct = default)
    {
        return Task.FromResult(new PaymentStatusResult
        {
            Success = true,
            Status = "completed"
        });
    }

    public Task<BinCheckResult> CheckBinAsync(string binNumber, CancellationToken ct = default)
    {
        return Task.FromResult(new BinCheckResult
        {
            Success = true,
            BinNumber = binNumber,
            CardType = "CREDIT_CARD",
            CardAssociation = "VISA"
        });
    }

    public Task<InstallmentResult> GetInstallmentOptionsAsync(string binNumber, decimal price, CancellationToken ct = default)
    {
        return Task.FromResult(new InstallmentResult
        {
            Success = true,
            Options = new List<InstallmentOption>()
        });
    }

    public bool ValidateWebhookSignature(string payload, string signature) => true;
}

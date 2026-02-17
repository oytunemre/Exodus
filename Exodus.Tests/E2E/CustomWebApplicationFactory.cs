using Exodus.Data;
using Exodus.Services.Email;
using Exodus.Services.PaymentGateway;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Exodus.Tests.E2E;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            // Add InMemory database with a unique name per factory instance
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("ExodusTestDb_" + Guid.NewGuid().ToString("N"));
            });

            // Replace email service with a no-op implementation
            var emailDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmailService));
            if (emailDescriptor != null)
                services.Remove(emailDescriptor);
            services.AddScoped<IEmailService, FakeEmailService>();

            // Replace payment gateway with a fake implementation
            var paymentDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IPaymentGateway));
            if (paymentDescriptor != null)
                services.Remove(paymentDescriptor);
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

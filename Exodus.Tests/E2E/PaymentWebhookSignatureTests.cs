using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Exodus.Data;
using Exodus.Models.Entities;
using Exodus.Models.Enums;
using Exodus.Services.PaymentGateway;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Exodus.Tests.E2E;

/// <summary>
/// The anonymous payment webhook must verify the gateway signature (header X-Webhook-Signature over the
/// raw request body) before it changes anything.
/// </summary>
public class PaymentWebhookSignatureTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string SignatureHeader = "X-Webhook-Signature";
    private readonly CustomWebApplicationFactory _factory;

    public PaymentWebhookSignatureTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Rejects every signature.</summary>
    private sealed class RejectingSignatureGateway : FakePaymentGateway
    {
        public override bool ValidateWebhookSignature(string payload, string signature) => false;
    }

    /// <summary>Accepts only the SHA-256 hex digest of the exact payload it is given.</summary>
    private sealed class DigestSignatureGateway : FakePaymentGateway
    {
        public static string Sign(string payload) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));

        public override bool ValidateWebhookSignature(string payload, string signature) => signature == Sign(payload);
    }

    private WebApplicationFactory<Program> WithGateway<TGateway>() where TGateway : class, IPaymentGateway =>
        _factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPaymentGateway>();
            services.AddScoped<IPaymentGateway, TGateway>();
        }));

    private WebApplicationFactory<Program> WithoutGateway() =>
        _factory.WithWebHostBuilder(b => b.ConfigureTestServices(services => services.RemoveAll<IPaymentGateway>()));

    /// <summary>A buyer, an order and a payment intent with an external reference, stored directly in the host's database.</summary>
    private static async Task<(int IntentId, string ExternalReference)> SeedIntentAsync(
        WebApplicationFactory<Program> factory, PaymentStatus status)
    {
        var token = Guid.NewGuid().ToString("N")[..10];
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var buyer = new Users { Name = "Webhook Buyer", Email = $"wh-{token}@test.com", Username = $"wh{token}", Password = "x", Role = UserRole.Customer };
        db.Users.Add(buyer);
        await db.SaveChangesAsync();

        var order = new Order { OrderNumber = $"ORD-WH-{token}", BuyerId = buyer.Id, Status = OrderStatus.Pending, SubTotal = 100m, TotalAmount = 100m };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var intent = new PaymentIntent
        {
            OrderId = order.Id,
            Amount = 100m,
            Method = PaymentMethod.CreditCard,
            Status = status,
            ExternalReference = $"PAY-WH-{token}"
        };
        db.PaymentIntents.Add(intent);
        await db.SaveChangesAsync();

        return (intent.Id, intent.ExternalReference);
    }

    private static async Task<PaymentIntent> GetIntentAsync(WebApplicationFactory<Program> factory, int intentId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.PaymentIntents.AsNoTracking().SingleAsync(p => p.Id == intentId);
    }

    private static string WebhookBody(string eventType, string externalReference) =>
        JsonSerializer.Serialize(new
        {
            payload = JsonSerializer.Serialize(new { eventType, externalReference, amount = 100m, message = "from test" })
        });

    private static Task<HttpResponseMessage> PostWebhookAsync(HttpClient client, string body, string? signature)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payment/webhook/iyzico")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        if (signature != null)
            request.Headers.Add(SignatureHeader, signature);
        return client.SendAsync(request);
    }

    [Theory]
    [InlineData("payment.captured", PaymentStatus.Authorized)]
    [InlineData("payment.refunded", PaymentStatus.Captured)]
    public async Task Webhook_InvalidSignature_ReturnsUnauthorized_AndIntentUnchanged(string eventType, PaymentStatus initial)
    {
        using var factory = WithGateway<RejectingSignatureGateway>();
        var (intentId, reference) = await SeedIntentAsync(factory, initial);

        var response = await PostWebhookAsync(factory.CreateClient(), WebhookBody(eventType, reference), "forged");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var intent = await GetIntentAsync(factory, intentId);
        intent.Status.Should().Be(initial);
        intent.RefundedAmount.Should().Be(0m);
    }

    [Fact]
    public async Task Webhook_NoGatewayRegistered_ReturnsUnauthorized_AndIntentUnchanged()
    {
        using var factory = WithoutGateway();
        var (intentId, reference) = await SeedIntentAsync(factory, PaymentStatus.Authorized);

        var response = await PostWebhookAsync(factory.CreateClient(), WebhookBody("payment.captured", reference), "anything");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await GetIntentAsync(factory, intentId)).Status.Should().Be(PaymentStatus.Authorized);
    }

    [Fact]
    public async Task Webhook_SignatureOverRawBody_IsAccepted_AndCaptures()
    {
        using var factory = WithGateway<DigestSignatureGateway>();
        var (intentId, reference) = await SeedIntentAsync(factory, PaymentStatus.Authorized);
        var client = factory.CreateClient();
        var body = WebhookBody("payment.captured", reference);

        var missing = await PostWebhookAsync(client, body, signature: null);
        var wrong = await PostWebhookAsync(client, body, DigestSignatureGateway.Sign(body + " "));
        missing.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await GetIntentAsync(factory, intentId)).Status.Should().Be(PaymentStatus.Authorized);

        var valid = await PostWebhookAsync(client, body, DigestSignatureGateway.Sign(body));

        valid.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetIntentAsync(factory, intentId)).Status.Should().Be(PaymentStatus.Captured);
    }

    [Fact]
    public async Task Webhook_ValidSignature_MalformedBody_ReturnsBadRequest()
    {
        using var factory = WithGateway<DigestSignatureGateway>();
        var client = factory.CreateClient();

        foreach (var body in new[] { "not json", "{}", "{\"payload\":\"\"}" })
        {
            var response = await PostWebhookAsync(client, body, DigestSignatureGateway.Sign(body));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        }
    }
}

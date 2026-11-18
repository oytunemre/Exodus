using System.Net;
using System.Net.Http.Json;
using Exodus.Controllers;
using Exodus.Models.Dto;
using Exodus.Models.Dto.CartDto;
using Exodus.Models.Dto.ListingDto;
using Exodus.Models.Dto.Payment;
using Exodus.Models.Dto.ProductDto;
using Exodus.Models.Enums;
using Exodus.Services.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Exodus.Tests.E2E;

public class PaymentAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PaymentAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Builds seller -> product -> listing -> customer -> cart -> address -> checkout -> payment intent.
    /// Returned client is authenticated as the order owner (customer).
    /// </summary>
    private async Task<(HttpClient Client, int OrderId, int IntentId)> SetupOwnerWithIntentAsync(
        string suffix, decimal price = 100.00m, bool with3DSCard = false)
    {
        var client = _factory.CreateClient();

        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(client, $"pauth{suffix}");
        var prodResp = await client.PostAsJsonAsync("/api/product", new AddProductDto
        {
            ProductName = $"PayAuth Product {suffix}",
            ProductDescription = "For payment authorization tests",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        }, TestHelper.JsonOptions);
        var prod = await prodResp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var lstResp = await client.PostAsJsonAsync("/api/listings", new AddListingDto
        {
            ProductId = prod!.Id,
            SellerId = seller.UserId,
            Price = price,
            Stock = 50,
            Condition = ListingCondition.New
        }, TestHelper.JsonOptions);
        var listing = await lstResp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);

        var owner = await TestHelper.RegisterAndLoginAsCustomerAsync(client, $"pauth{suffix}");
        await client.PostAsJsonAsync("/api/cart/add",
            new AddToCartDto { UserId = owner.UserId, ListingId = listing!.Id, Quantity = 1 },
            TestHelper.JsonOptions);

        var addrResp = await client.PostAsJsonAsync("/api/address", new CreateAddressDto
        {
            Title = "Home",
            FullName = "Test Customer",
            Phone = "+905551234567",
            City = "Istanbul",
            District = "Kadikoy",
            AddressLine = "Test Mah. Test Sok. No:1"
        }, TestHelper.JsonOptions);
        var address = await addrResp.Content.ReadFromJsonAsync<AddressResponseDto>(TestHelper.JsonOptions);

        var orderResp = await client.PostAsJsonAsync("/api/order/checkout",
            new CreateOrderDto { ShippingAddressId = address!.Id }, TestHelper.JsonOptions);
        var order = await orderResp.Content.ReadFromJsonAsync<OrderDetailResponseDto>(TestHelper.JsonOptions);

        var payResp = await client.PostAsJsonAsync("/api/payment/intents", new CreatePaymentIntentDto
        {
            OrderId = order!.Id,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY",
            CardDetails = with3DSCard
                ? new CardDetailsDto
                {
                    CardNumber = "5528790000000008",
                    ExpiryDate = "12/30",
                    Cvv = "123",
                    CardHolderName = "Made Up"
                }
                : null
        }, TestHelper.JsonOptions);
        payResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var intent = await payResp.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);

        return (client, order.Id, intent!.Id);
    }

    private async Task<HttpClient> NewCustomerClientAsync(string suffix)
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, suffix);
        return client;
    }

    [Fact]
    public async Task CreateIntent_ForForeignOrder_ReturnsForbidden()
    {
        var (_, orderId, _) = await SetupOwnerWithIntentAsync("ci");
        // Owner's intent already exists: the ownership check must run before the "existing intent" shortcut.
        var other = await NewCustomerClientAsync("pauthcib");

        var response = await other.PostAsJsonAsync("/api/payment/intents", new CreatePaymentIntentDto
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        }, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("intent")]
    [InlineData("order")]
    [InlineData("events")]
    public async Task GetIntent_ForForeignOrder_ReturnsForbidden(string route)
    {
        var (_, orderId, intentId) = await SetupOwnerWithIntentAsync($"g{route}");
        var other = await NewCustomerClientAsync($"pauthg{route}b");

        var url = RouteFor(route, orderId, intentId);
        var response = await other.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("intent")]
    [InlineData("order")]
    [InlineData("events")]
    public async Task GetIntent_AsOwner_ReturnsOk(string route)
    {
        var (owner, orderId, intentId) = await SetupOwnerWithIntentAsync($"o{route}");

        var response = await owner.GetAsync(RouteFor(route, orderId, intentId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("intent")]
    [InlineData("order")]
    [InlineData("events")]
    public async Task GetIntent_AsAdmin_ForeignOrder_ReturnsOk(string route)
    {
        var (_, orderId, intentId) = await SetupOwnerWithIntentAsync($"a{route}");
        var admin = await TestHelper.CreateAdminClientAsync(_factory);

        var response = await admin.GetAsync(RouteFor(route, orderId, intentId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Cancel_ForForeignOrder_ReturnsForbidden()
    {
        var (_, _, intentId) = await SetupOwnerWithIntentAsync("cn");
        var other = await NewCustomerClientAsync("pauthcnb");

        var response = await other.PostAsJsonAsync($"/api/payment/intents/{intentId}/cancel",
            new CancelPaymentDto { Reason = "x" }, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Confirm3DSecure_ForForeignOrder_ReturnsForbidden()
    {
        var (_, _, intentId) = await SetupOwnerWithIntentAsync("3d", 600m, with3DSCard: true);
        var other = await NewCustomerClientAsync("pauth3db");

        var response = await other.PostAsJsonAsync($"/api/payment/intents/{intentId}/3ds/confirm",
            new Confirm3DSecureDto { AuthenticationResult = "success" }, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Confirm3DSecure_AsOwnerCustomer_ReturnsForbidden_AndIntentStaysPending()
    {
        var (owner, _, intentId) = await SetupOwnerWithIntentAsync("3do", 600m, with3DSCard: true);

        var before = await (await owner.GetAsync($"/api/payment/intents/{intentId}"))
            .Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        before!.Status.Should().Be(PaymentStatus.Pending);

        var response = await owner.PostAsJsonAsync($"/api/payment/intents/{intentId}/3ds/confirm",
            new Confirm3DSecureDto { AuthenticationResult = "success" }, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var after = await (await owner.GetAsync($"/api/payment/intents/{intentId}"))
            .Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        after!.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task Confirm3DSecure_AsAdmin_InDevelopment_Captures()
    {
        var (_, _, intentId) = await SetupOwnerWithIntentAsync("3da", 600m, with3DSCard: true);
        var admin = await TestHelper.CreateAdminClientAsync(_factory);

        var response = await admin.PostAsJsonAsync($"/api/payment/intents/{intentId}/3ds/confirm",
            new Confirm3DSecureDto { AuthenticationResult = "success" }, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        result!.Status.Should().Be(PaymentStatus.Captured);
    }

    [Theory]
    [InlineData("gateway/process")]
    [InlineData("gateway/3ds/initialize")]
    public async Task Gateway_ForForeignOrder_ReturnsForbidden(string path)
    {
        var (_, orderId, _) = await SetupOwnerWithIntentAsync($"gw{path.Length}");
        var other = await NewCustomerClientAsync($"pauthgw{path.Length}b");

        var response = await other.PostAsJsonAsync($"/api/payment/{path}", new ProcessGatewayPaymentDto
        {
            OrderId = orderId,
            Card = new GatewayCardDto
            {
                CardHolderName = "Other",
                CardNumber = "5528790000000008",
                ExpireMonth = "12",
                ExpireYear = "2030",
                Cvc = "123"
            }
        }, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("authorize")]
    [InlineData("capture")]
    [InlineData("refund")]
    public async Task Capture_AsCustomer_ReturnsForbidden(string action)
    {
        var (owner, _, intentId) = await SetupOwnerWithIntentAsync($"adm{action}");

        var response = await owner.PostAsJsonAsync($"/api/payment/intents/{intentId}/{action}",
            new { reason = "x" }, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SimulateFail_AsCustomer_ReturnsForbidden()
    {
        var (owner, _, intentId) = await SetupOwnerWithIntentAsync("sf");

        var response = await owner.PostAsJsonAsync($"/api/payment/intents/{intentId}/simulate-fail",
            new SimulatePaymentDto { Reason = "x" }, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SimulateSuccess_AsCustomer_ReturnsForbidden()
    {
        var (owner, _, intentId) = await SetupOwnerWithIntentAsync("ss");

        var response = await owner.PostAsJsonAsync($"/api/payment/intents/{intentId}/simulate-success",
            new SimulatePaymentDto { Reason = "x" }, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SimulateSuccess_AsAdmin_InDevelopment_CapturesPayment()
    {
        var (_, _, intentId) = await SetupOwnerWithIntentAsync("ssa");
        var admin = await TestHelper.CreateAdminClientAsync(_factory);

        var response = await admin.PostAsJsonAsync($"/api/payment/intents/{intentId}/simulate-success",
            new SimulatePaymentDto { Reason = "x" }, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentIntentResponseDto>(TestHelper.JsonOptions);
        result!.Status.Should().Be(PaymentStatus.Captured);
    }

    [Fact]
    public async Task DevelopmentOnlyAttribute_InProduction_ReturnsNotFound()
    {
        var services = new ServiceCollection()
            .AddSingleton<IWebHostEnvironment>(new StubEnvironment("Production"))
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(),
            new Dictionary<string, object?>(), controller: new object());

        new DevelopmentOnlyAttribute().OnActionExecuting(context);

        context.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DevelopmentOnlyAttribute_InDevelopment_AllowsRequest()
    {
        var services = new ServiceCollection()
            .AddSingleton<IWebHostEnvironment>(new StubEnvironment("Development"))
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(),
            new Dictionary<string, object?>(), controller: new object());

        new DevelopmentOnlyAttribute().OnActionExecuting(context);

        context.Result.Should().BeNull();
        await Task.CompletedTask;
    }

    private static string RouteFor(string route, int orderId, int intentId) => route switch
    {
        "intent" => $"/api/payment/intents/{intentId}",
        "order" => $"/api/payment/order/{orderId}",
        "events" => $"/api/payment/intents/{intentId}/events",
        _ => throw new ArgumentOutOfRangeException(nameof(route))
    };

    private sealed class StubEnvironment : IWebHostEnvironment
    {
        public StubEnvironment(string environmentName) => EnvironmentName = environmentName;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Exodus";
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

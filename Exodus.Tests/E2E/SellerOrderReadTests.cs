using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Exodus.Models.Dto;
using Exodus.Models.Dto.CartDto;
using Exodus.Models.Dto.ListingDto;
using Exodus.Models.Dto.ProductDto;
using Exodus.Models.Enums;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class SellerOrderReadTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SellerOrderReadTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private sealed record Scenario(
        AuthResponseDto Seller, AuthResponseDto OtherSeller, AuthResponseDto Customer,
        OrderDetailResponseDto Order, int SellerOrderId, int ListingId);

    private HttpClient ClientFor(AuthResponseDto user)
    {
        var client = _factory.CreateClient();
        TestHelper.SetAuthToken(client, user.Token!);
        return client;
    }

    private async Task<int> CreateListingAsync(AuthResponseDto seller, string name)
    {
        var client = ClientFor(seller);
        var prodResp = await client.PostAsJsonAsync("/api/product", new AddProductDto
        {
            ProductName = name,
            ProductDescription = "Seller order read tests",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        }, TestHelper.JsonOptions);
        var prod = await prodResp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);

        var lstResp = await client.PostAsJsonAsync("/api/listings", new AddListingDto
        {
            ProductId = prod!.Id,
            SellerId = seller.UserId,
            Price = 100m,
            Stock = 50,
            Condition = ListingCondition.New
        }, TestHelper.JsonOptions);
        var listing = await lstResp.Content.ReadFromJsonAsync<ListingResponseDto>(TestHelper.JsonOptions);
        return listing!.Id;
    }

    private async Task<Scenario> CreateScenarioAsync()
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var setup = _factory.CreateClient();
        var seller = await TestHelper.RegisterAndLoginAsSellerAsync(setup, $"sor{s}a");
        var other = await TestHelper.RegisterAndLoginAsSellerAsync(setup, $"sor{s}b");
        var listingId = await CreateListingAsync(seller, $"SOR {s}");

        var client = _factory.CreateClient();
        var customer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, $"sor{s}");
        await client.PostAsJsonAsync("/api/cart/add",
            new AddToCartDto { UserId = customer.UserId, ListingId = listingId, Quantity = 1 }, TestHelper.JsonOptions);

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

        var soId = order!.SellerOrders.Single(x => x.SellerId == seller.UserId).Id;
        return new Scenario(seller, other, customer, order, soId, listingId);
    }

    private async Task<JsonElement> GetListEntryAsync(Scenario sc)
    {
        var resp = await ClientFor(sc.Seller).GetAsync("/api/seller/orders");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return list.EnumerateArray().Single(e => e.GetProperty("id").GetInt32() == sc.SellerOrderId);
    }

    [Fact]
    public async Task GetSellerOrders_IncludesOrderNumberCreatedAtOrderIdAndListingIds()
    {
        var sc = await CreateScenarioAsync();

        var entry = await GetListEntryAsync(sc);

        entry.GetProperty("orderId").GetInt32().Should().Be(sc.Order.Id);
        entry.GetProperty("orderNumber").GetString().Should().Be(sc.Order.OrderNumber);
        entry.GetProperty("createdAt").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(2));
        entry.GetProperty("items")[0].GetProperty("listingId").GetInt32().Should().Be(sc.ListingId);
        entry.GetProperty("status").GetString().Should().Be("Pending");
        entry.GetProperty("shipment").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetSellerOrders_ShipmentPopulatedAfterShip()
    {
        var sc = await CreateScenarioAsync();
        var ship = await ClientFor(sc.Seller).PostAsJsonAsync($"/api/seller/orders/{sc.SellerOrderId}/ship",
            new { carrier = "Yurtici", trackingNumber = "TR123" }, TestHelper.JsonOptions);
        ship.StatusCode.Should().Be(HttpStatusCode.OK);

        var entry = await GetListEntryAsync(sc);

        var shipment = entry.GetProperty("shipment");
        shipment.GetProperty("carrier").GetString().Should().Be("Yurtici");
        shipment.GetProperty("trackingNumber").GetString().Should().Be("TR123");
    }

    [Fact]
    public async Task GetSellerOrderById_Owner_ReturnsSameShapeAsList()
    {
        var sc = await CreateScenarioAsync();

        var resp = await ClientFor(sc.Seller).GetAsync($"/api/seller/orders/{sc.SellerOrderId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var single = await resp.Content.ReadFromJsonAsync<JsonElement>();
        single.GetProperty("orderNumber").GetString().Should().Be(sc.Order.OrderNumber);
        single.GetProperty("items")[0].GetProperty("listingId").GetInt32().Should().Be(sc.ListingId);
        single.GetRawText().Should().Be((await GetListEntryAsync(sc)).GetRawText());
    }

    [Fact]
    public async Task GetSellerOrderById_OtherSeller_Returns404()
    {
        var sc = await CreateScenarioAsync();

        var resp = await ClientFor(sc.OtherSeller).GetAsync($"/api/seller/orders/{sc.SellerOrderId}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSellerOrderById_Customer_Returns403()
    {
        var sc = await CreateScenarioAsync();

        var resp = await ClientFor(sc.Customer).GetAsync($"/api/seller/orders/{sc.SellerOrderId}");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

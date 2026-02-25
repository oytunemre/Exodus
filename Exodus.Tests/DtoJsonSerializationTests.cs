using System.Text.Json;
using System.Text.Json.Serialization;
using Exodus.Models.Dto;
using Exodus.Models.Dto.CartDto;
using Exodus.Models.Dto.ListingDto;
using Exodus.Models.Dto.OrderDto;
using Exodus.Models.Dto.ProductDto;
using Exodus.Models.Enums;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests;

public class DtoJsonSerializationTests
{
    private static readonly string ProjectRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static string GetDtoJsonPath(string relativePath) =>
        Path.Combine(ProjectRoot, "Exodus.Tests", "TestData", "Dto", relativePath);

    private static string LoadJson(string relativePath)
    {
        var path = GetDtoJsonPath(relativePath);
        File.Exists(path).Should().BeTrue($"DTO JSON fixture dosyasi bulunamadi: {path}");
        return File.ReadAllText(path);
    }

    #region JSON Fixture File Validation

    [Theory]
    [InlineData("Auth/register-request.json")]
    [InlineData("Auth/login-request.json")]
    [InlineData("Auth/auth-response.json")]
    [InlineData("Auth/refresh-token-request.json")]
    [InlineData("Address/create-address-request.json")]
    [InlineData("Address/address-response.json")]
    [InlineData("Product/add-product-request.json")]
    [InlineData("Product/product-response.json")]
    [InlineData("Listing/add-listing-request.json")]
    [InlineData("Listing/listing-response.json")]
    [InlineData("Cart/add-to-cart-request.json")]
    [InlineData("Cart/cart-response.json")]
    [InlineData("Order/checkout-request.json")]
    [InlineData("Order/order-detail-response.json")]
    [InlineData("Order/order-list-response.json")]
    [InlineData("Order/cancel-order-request.json")]
    [InlineData("Order/refund-request.json")]
    public void DtoJsonFixture_ShouldBeValidJson(string relativePath)
    {
        var content = LoadJson(relativePath);

        var act = () => JsonDocument.Parse(content);
        act.Should().NotThrow($"{relativePath} gecerli bir JSON olmali");
    }

    [Theory]
    [InlineData("Auth/register-request.json")]
    [InlineData("Auth/login-request.json")]
    [InlineData("Auth/auth-response.json")]
    [InlineData("Auth/refresh-token-request.json")]
    [InlineData("Address/create-address-request.json")]
    [InlineData("Address/address-response.json")]
    [InlineData("Product/add-product-request.json")]
    [InlineData("Product/product-response.json")]
    [InlineData("Listing/add-listing-request.json")]
    [InlineData("Listing/listing-response.json")]
    [InlineData("Cart/add-to-cart-request.json")]
    [InlineData("Cart/cart-response.json")]
    [InlineData("Order/checkout-request.json")]
    [InlineData("Order/order-detail-response.json")]
    [InlineData("Order/order-list-response.json")]
    [InlineData("Order/cancel-order-request.json")]
    [InlineData("Order/refund-request.json")]
    public void DtoJsonFixture_RootShouldBeObject(string relativePath)
    {
        var content = LoadJson(relativePath);
        using var doc = JsonDocument.Parse(content);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object,
            $"{relativePath} kok elemani bir JSON nesnesi olmali");
    }

    [Theory]
    [InlineData("Auth/register-request.json")]
    [InlineData("Auth/login-request.json")]
    [InlineData("Auth/auth-response.json")]
    [InlineData("Auth/refresh-token-request.json")]
    [InlineData("Address/create-address-request.json")]
    [InlineData("Address/address-response.json")]
    [InlineData("Product/add-product-request.json")]
    [InlineData("Product/product-response.json")]
    [InlineData("Listing/add-listing-request.json")]
    [InlineData("Listing/listing-response.json")]
    [InlineData("Cart/add-to-cart-request.json")]
    [InlineData("Cart/cart-response.json")]
    [InlineData("Order/checkout-request.json")]
    [InlineData("Order/order-detail-response.json")]
    [InlineData("Order/order-list-response.json")]
    [InlineData("Order/cancel-order-request.json")]
    [InlineData("Order/refund-request.json")]
    public void DtoJsonFixture_PropertyNamesShouldBeCamelCase(string relativePath)
    {
        var content = LoadJson(relativePath);
        using var doc = JsonDocument.Parse(content);

        foreach (var property in doc.RootElement.EnumerateObject())
        {
            var name = property.Name;
            if (name.Length > 0)
            {
                char.IsLower(name[0]).Should().BeTrue(
                    $"'{relativePath}' icindeki '{name}' ozelligi camelCase olmali (kucuk harfle baslamali)");
            }
        }
    }

    #endregion

    #region Auth DTO Deserialization Tests

    [Fact]
    public void RegisterRequestJson_ShouldDeserializeToRegisterDto()
    {
        var content = LoadJson("Auth/register-request.json");

        var dto = JsonSerializer.Deserialize<RegisterDto>(content, JsonOptions);

        dto.Should().NotBeNull();
        dto!.Name.Should().NotBeNullOrWhiteSpace("name alani olmali");
        dto.Email.Should().Contain("@", "email gecerli bir e-posta olmali");
        dto.Username.Should().NotBeNullOrWhiteSpace("username alani olmali");
        dto.Password.Should().NotBeNullOrWhiteSpace("password alani olmali");
    }

    [Fact]
    public void LoginRequestJson_ShouldDeserializeToLoginDto()
    {
        var content = LoadJson("Auth/login-request.json");

        var dto = JsonSerializer.Deserialize<LoginDto>(content, JsonOptions);

        dto.Should().NotBeNull();
        dto!.EmailOrUsername.Should().NotBeNullOrWhiteSpace("emailOrUsername alani olmali");
        dto.Password.Should().NotBeNullOrWhiteSpace("password alani olmali");
    }

    [Fact]
    public void AuthResponseJson_ShouldDeserializeToAuthResponseDto()
    {
        var content = LoadJson("Auth/auth-response.json");

        var dto = JsonSerializer.Deserialize<AuthResponseDto>(content, JsonOptions);

        dto.Should().NotBeNull();
        dto!.UserId.Should().BeGreaterThan(0, "userId pozitif olmali");
        dto.Name.Should().NotBeNullOrWhiteSpace("name alani olmali");
        dto.Email.Should().Contain("@", "email gecerli bir e-posta olmali");
        dto.Username.Should().NotBeNullOrWhiteSpace("username alani olmali");
    }

    [Fact]
    public void RefreshTokenRequestJson_ShouldDeserializeToRefreshTokenRequestDto()
    {
        var content = LoadJson("Auth/refresh-token-request.json");

        var dto = JsonSerializer.Deserialize<RefreshTokenRequestDto>(content, JsonOptions);

        dto.Should().NotBeNull();
        dto!.RefreshToken.Should().NotBeNullOrWhiteSpace("refreshToken alani olmali");
    }

    #endregion

    #region Address DTO Deserialization Tests

    [Fact]
    public void CreateAddressRequestJson_ShouldDeserializeToCreateAddressDto()
    {
        var content = LoadJson("Address/create-address-request.json");

        var dto = JsonSerializer.Deserialize<CreateAddressDto>(content, JsonOptions);

        dto.Should().NotBeNull();
        dto!.Title.Should().NotBeNullOrWhiteSpace("title alani olmali");
        dto.FullName.Should().NotBeNullOrWhiteSpace("fullName alani olmali");
        dto.Phone.Should().NotBeNullOrWhiteSpace("phone alani olmali");
        dto.City.Should().NotBeNullOrWhiteSpace("city alani olmali");
        dto.District.Should().NotBeNullOrWhiteSpace("district alani olmali");
        dto.AddressLine.Should().NotBeNullOrWhiteSpace("addressLine alani olmali");
    }

    [Fact]
    public void AddressResponseJson_ShouldDeserializeToAddressResponseDto()
    {
        var content = LoadJson("Address/address-response.json");

        var dto = JsonSerializer.Deserialize<AddressResponseDto>(content, JsonOptions);

        dto.Should().NotBeNull();
        dto!.Id.Should().BeGreaterThan(0, "id pozitif olmali");
        dto.Title.Should().NotBeNullOrWhiteSpace("title alani olmali");
        dto.City.Should().NotBeNullOrWhiteSpace("city alani olmali");
    }

    #endregion

    #region Product DTO Deserialization Tests

    [Fact]
    public void AddProductRequestJson_ShouldDeserializeToAddProductDto()
    {
        var content = LoadJson("Product/add-product-request.json");

        var dto = JsonSerializer.Deserialize<AddProductDto>(content, JsonOptions);

        dto.Should().NotBeNull();
        dto!.ProductName.Should().NotBeNullOrWhiteSpace("productName alani olmali");
        dto.ProductDescription.Should().NotBeNullOrWhiteSpace("productDescription alani olmali");
        dto.Barcodes.Should().NotBeEmpty("barcodes en az bir deger icermeli");
    }

    [Fact]
    public void ProductResponseJson_ShouldDeserializeToProductResponseDto()
    {
        var content = LoadJson("Product/product-response.json");

        var dto = JsonSerializer.Deserialize<ProductResponseDto>(content, JsonOptions);

        dto.Should().NotBeNull();
        dto!.Id.Should().BeGreaterThan(0, "id pozitif olmali");
        dto.ProductName.Should().NotBeNullOrWhiteSpace("productName alani olmali");
    }

    #endregion

    #region Listing DTO Deserialization Tests

    [Fact]
    public void AddListingRequestJson_ShouldDeserializeToAddListingDto()
    {
        var content = LoadJson("Listing/add-listing-request.json");

        var dto = JsonSerializer.Deserialize<AddListingDto>(content, JsonOptions);

        dto.Should().NotBeNull();
        dto!.ProductId.Should().BeGreaterThan(0, "productId pozitif olmali");
        dto.SellerId.Should().BeGreaterThan(0, "sellerId pozitif olmali");
        dto.Price.Should().BeGreaterThan(0, "price pozitif olmali");
        dto.Stock.Should().BeGreaterThanOrEqualTo(0, "stock negatif olmamali");
    }

    [Fact]
    public void ListingResponseJson_ShouldDeserializeToListingResponseDto()
    {
        var content = LoadJson("Listing/listing-response.json");

        var dto = JsonSerializer.Deserialize<ListingResponseDto>(content, JsonOptions);

        dto.Should().NotBeNull();
        dto!.Id.Should().BeGreaterThan(0, "id pozitif olmali");
        dto.Price.Should().BeGreaterThan(0, "price pozitif olmali");
        dto.Stock.Should().BeGreaterThanOrEqualTo(0, "stock negatif olmamali");
    }

    #endregion

    #region Cart DTO Deserialization Tests

    [Fact]
    public void AddToCartRequestJson_ShouldDeserializeToAddToCartDto()
    {
        var content = LoadJson("Cart/add-to-cart-request.json");

        var dto = JsonSerializer.Deserialize<AddToCartDto>(content, JsonOptions);

        dto.Should().NotBeNull();
        dto!.UserId.Should().BeGreaterThan(0, "userId pozitif olmali");
        dto.ListingId.Should().BeGreaterThan(0, "listingId pozitif olmali");
        dto.Quantity.Should().BeGreaterThan(0, "quantity pozitif olmali");
    }

    [Fact]
    public void CartResponseJson_ShouldDeserializeToCartResponseDto()
    {
        var content = LoadJson("Cart/cart-response.json");

        var dto = JsonSerializer.Deserialize<CartResponseDto>(content, JsonOptions);

        dto.Should().NotBeNull();
        dto!.CartId.Should().BeGreaterThan(0, "cartId pozitif olmali");
        dto.UserId.Should().BeGreaterThan(0, "userId pozitif olmali");
        dto.CartTotal.Should().BeGreaterThanOrEqualTo(0, "cartTotal negatif olmamali");
        dto.Items.Should().NotBeNull("items null olmamali");
    }

    [Fact]
    public void CartResponseJson_ItemsShouldHaveCorrectStructure()
    {
        var content = LoadJson("Cart/cart-response.json");

        var dto = JsonSerializer.Deserialize<CartResponseDto>(content, JsonOptions);

        dto.Should().NotBeNull();
        dto!.Items.Should().NotBeEmpty("test verisinde en az bir urun olmali");

        var item = dto.Items.First();
        item.CartItemId.Should().BeGreaterThan(0);
        item.ListingId.Should().BeGreaterThan(0);
        item.ProductId.Should().BeGreaterThan(0);
        item.ProductName.Should().NotBeNullOrWhiteSpace();
        item.UnitPrice.Should().BeGreaterThan(0);
        item.Quantity.Should().BeGreaterThan(0);
        item.LineTotal.Should().BeGreaterThan(0);
    }

    #endregion

    #region Order DTO Deserialization Tests

    [Fact]
    public void CheckoutRequestJson_ShouldDeserializeToCreateOrderDto()
    {
        var content = LoadJson("Order/checkout-request.json");

        var dto = JsonSerializer.Deserialize<CreateOrderDto>(content, JsonOptions);

        dto.Should().NotBeNull();
        dto!.ShippingAddressId.Should().BeGreaterThan(0, "shippingAddressId pozitif olmali");
    }

    [Fact]
    public void OrderDetailResponseJson_ShouldDeserializeToOrderDetailResponseDto()
    {
        var content = LoadJson("Order/order-detail-response.json");

        var dto = JsonSerializer.Deserialize<OrderDetailResponseDto>(content, JsonOptions);

        dto.Should().NotBeNull();
        dto!.Id.Should().BeGreaterThan(0, "id pozitif olmali");
        dto.OrderNumber.Should().NotBeNullOrWhiteSpace("orderNumber alani olmali");
        dto.Currency.Should().NotBeNullOrWhiteSpace("currency alani olmali");
        dto.TotalAmount.Should().BeGreaterThanOrEqualTo(0, "totalAmount negatif olmamali");
        dto.SellerOrders.Should().NotBeNull("sellerOrders null olmamali");
        dto.Events.Should().NotBeNull("events null olmamali");
    }

    [Fact]
    public void OrderDetailResponseJson_OrderNumberShouldHaveCorrectFormat()
    {
        var content = LoadJson("Order/order-detail-response.json");
        using var doc = JsonDocument.Parse(content);

        doc.RootElement.TryGetProperty("orderNumber", out var orderNumber).Should().BeTrue(
            "orderNumber alani olmali");

        var orderNumberValue = orderNumber.GetString();
        orderNumberValue.Should().NotBeNullOrWhiteSpace("orderNumber bos olmamali");
        orderNumberValue.Should().StartWith("ORD-", "orderNumber 'ORD-' ile baslamali");
        orderNumberValue!.Length.Should().BeLessThanOrEqualTo(25,
            "orderNumber max 25 karakter olmali");
    }

    [Fact]
    public void OrderListResponseJson_ShouldDeserializeToOrderListResponseDto()
    {
        var content = LoadJson("Order/order-list-response.json");

        var dto = JsonSerializer.Deserialize<OrderListResponseDto>(content, JsonOptions);

        dto.Should().NotBeNull();
        dto!.Items.Should().NotBeNull("items null olmamali");
        dto.Page.Should().BeGreaterThan(0, "page pozitif olmali");
        dto.PageSize.Should().BeGreaterThan(0, "pageSize pozitif olmali");
        dto.TotalCount.Should().BeGreaterThanOrEqualTo(0, "totalCount negatif olmamali");
    }

    [Fact]
    public void CancelOrderRequestJson_ShouldDeserializeToCancelOrderDto()
    {
        var content = LoadJson("Order/cancel-order-request.json");

        var dto = JsonSerializer.Deserialize<CancelOrderDto>(content, JsonOptions);

        dto.Should().NotBeNull();
        dto!.Reason.Should().BeOneOf(Enum.GetValues<CancellationReason>(),
            "reason gecerli bir CancellationReason olmali");
    }

    [Fact]
    public void RefundRequestJson_ShouldDeserializeToRefundRequestDto()
    {
        var content = LoadJson("Order/refund-request.json");

        var dto = JsonSerializer.Deserialize<RefundRequestDto>(content, JsonOptions);

        dto.Should().NotBeNull();
        dto!.Reason.Should().NotBeNullOrWhiteSpace("reason alani olmali");
    }

    #endregion

    #region Roundtrip Serialization Tests

    [Fact]
    public void RegisterDto_RoundtripSerialization_ShouldPreserveValues()
    {
        var original = new RegisterDto
        {
            Name = "Test User",
            Email = "test@example.com",
            Username = "testuser",
            Password = "Test123!@#",
            Role = UserRole.Customer
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<RegisterDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be(original.Name);
        deserialized.Email.Should().Be(original.Email);
        deserialized.Username.Should().Be(original.Username);
        deserialized.Role.Should().Be(original.Role);
    }

    [Fact]
    public void CreateOrderDto_RoundtripSerialization_ShouldPreserveValues()
    {
        var original = new CreateOrderDto
        {
            ShippingAddressId = 1,
            BillingAddressId = 2,
            CustomerNote = "Test note",
            CouponCode = "DISCOUNT10"
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CreateOrderDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.ShippingAddressId.Should().Be(original.ShippingAddressId);
        deserialized.BillingAddressId.Should().Be(original.BillingAddressId);
        deserialized.CustomerNote.Should().Be(original.CustomerNote);
        deserialized.CouponCode.Should().Be(original.CouponCode);
    }

    [Fact]
    public void AddListingDto_RoundtripSerialization_ShouldPreserveValues()
    {
        var original = new AddListingDto
        {
            ProductId = 1,
            SellerId = 1,
            Price = 54999.99m,
            Stock = 25,
            Condition = ListingCondition.New
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<AddListingDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.ProductId.Should().Be(original.ProductId);
        deserialized.Price.Should().Be(original.Price);
        deserialized.Stock.Should().Be(original.Stock);
        deserialized.Condition.Should().Be(original.Condition);
    }

    [Fact]
    public void OrderListResponseDto_RoundtripSerialization_ShouldPreserveValues()
    {
        var original = new OrderListResponseDto
        {
            Items = new List<OrderListItemDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<OrderListResponseDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Page.Should().Be(original.Page);
        deserialized.PageSize.Should().Be(original.PageSize);
        deserialized.TotalCount.Should().Be(original.TotalCount);
    }

    #endregion

    #region JSON Property Name Tests

    [Fact]
    public void RegisterDto_SerializedJson_ShouldUseCamelCase()
    {
        var dto = new RegisterDto
        {
            Name = "Test",
            Email = "test@example.com",
            Username = "testuser",
            Password = "Test123!",
            Role = UserRole.Customer
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("name", out _).Should().BeTrue("'name' camelCase olmali");
        doc.RootElement.TryGetProperty("email", out _).Should().BeTrue("'email' camelCase olmali");
        doc.RootElement.TryGetProperty("username", out _).Should().BeTrue("'username' camelCase olmali");
        doc.RootElement.TryGetProperty("password", out _).Should().BeTrue("'password' camelCase olmali");
        doc.RootElement.TryGetProperty("role", out _).Should().BeTrue("'role' camelCase olmali");

        doc.RootElement.TryGetProperty("Name", out _).Should().BeFalse("'Name' PascalCase olmamali");
        doc.RootElement.TryGetProperty("Email", out _).Should().BeFalse("'Email' PascalCase olmamali");
    }

    [Fact]
    public void OrderListResponseDto_SerializedJson_ShouldUseCamelCase()
    {
        var dto = new OrderListResponseDto
        {
            Items = new List<OrderListItemDto>(),
            TotalCount = 5,
            Page = 1,
            PageSize = 20
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("items", out _).Should().BeTrue("'items' camelCase olmali");
        doc.RootElement.TryGetProperty("totalCount", out _).Should().BeTrue("'totalCount' camelCase olmali");
        doc.RootElement.TryGetProperty("page", out _).Should().BeTrue("'page' camelCase olmali");
        doc.RootElement.TryGetProperty("pageSize", out _).Should().BeTrue("'pageSize' camelCase olmali");
    }

    [Fact]
    public void CartResponseDto_SerializedJson_ShouldUseCamelCase()
    {
        var dto = new CartResponseDto
        {
            CartId = 1,
            UserId = 1,
            CartTotal = 100m,
            Items = new List<CartItemResponseDto>()
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("cartId", out _).Should().BeTrue("'cartId' camelCase olmali");
        doc.RootElement.TryGetProperty("userId", out _).Should().BeTrue("'userId' camelCase olmali");
        doc.RootElement.TryGetProperty("cartTotal", out _).Should().BeTrue("'cartTotal' camelCase olmali");
        doc.RootElement.TryGetProperty("items", out _).Should().BeTrue("'items' camelCase olmali");
    }

    #endregion
}

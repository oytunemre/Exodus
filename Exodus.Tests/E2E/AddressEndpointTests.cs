using System.Net;
using System.Net.Http.Json;
using Exodus.Models.Dto;
using Exodus.Models.Entities;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class AddressEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AddressEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static CreateAddressDto MakeAddress(string suffix = "") => new()
    {
        Title = "Home " + suffix,
        FullName = "Test User " + suffix,
        Phone = "+905551234567",
        City = "Istanbul",
        District = "Kadikoy",
        Neighborhood = "Caferaga",
        AddressLine = "Test St. No:1 " + suffix,
        PostalCode = "34710",
        IsDefault = false,
        Type = AddressType.Shipping
    };

    [Fact]
    public async Task GetAddresses_WithAuth_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "addrlist");

        var response = await client.GetAsync("/api/address");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAddresses_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/address");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAddress_WithValidData_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "addrcreate");

        var dto = MakeAddress("create");

        var response = await client.PostAsJsonAsync("/api/address", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<AddressResponseDto>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
        result!.Title.Should().Be("Home create");
        result.City.Should().Be("Istanbul");
        result.District.Should().Be("Kadikoy");
    }

    [Fact]
    public async Task CreateAddress_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = MakeAddress("noauth");

        var response = await client.PostAsJsonAsync("/api/address", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAddressById_ReturnsAddress()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "addrget");

        var dto = MakeAddress("getbyid");
        var createResp = await client.PostAsJsonAsync("/api/address", dto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<AddressResponseDto>(TestHelper.JsonOptions);

        var response = await client.GetAsync($"/api/address/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AddressResponseDto>(TestHelper.JsonOptions);
        result!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task UpdateAddress_ReturnsUpdated()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "addrupd");

        var dto = MakeAddress("update");
        var createResp = await client.PostAsJsonAsync("/api/address", dto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<AddressResponseDto>(TestHelper.JsonOptions);

        var updateDto = new UpdateAddressDto { Title = "Office Updated" };
        var response = await client.PutAsJsonAsync($"/api/address/{created!.Id}", updateDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AddressResponseDto>(TestHelper.JsonOptions);
        result!.Title.Should().Be("Office Updated");
    }

    [Fact]
    public async Task DeleteAddress_ReturnsNoContent()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "addrdel");

        var dto = MakeAddress("delete");
        var createResp = await client.PostAsJsonAsync("/api/address", dto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<AddressResponseDto>(TestHelper.JsonOptions);

        var response = await client.DeleteAsync($"/api/address/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetDefaultAddress_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "addrdef");

        var dto = MakeAddress("default");
        var createResp = await client.PostAsJsonAsync("/api/address", dto, TestHelper.JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<AddressResponseDto>(TestHelper.JsonOptions);

        var response = await client.PatchAsync($"/api/address/{created!.Id}/set-default", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

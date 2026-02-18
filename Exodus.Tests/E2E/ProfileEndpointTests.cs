using System.Net;
using System.Net.Http.Json;
using Exodus.Models.Dto;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class ProfileEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProfileEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProfile_WithAuth_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "profget");

        var response = await client.GetAsync("/api/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UserProfileResponseDto>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
        result!.Email.Should().Be("customerprofget@example.com");
        result.Username.Should().Be("customeruserprofget");
    }

    [Fact]
    public async Task GetProfile_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_WithValidData_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "profupd");

        var dto = new UpdateProfileDto
        {
            Name = "Updated Name",
            Phone = "+905559876543"
        };

        var response = await client.PutAsJsonAsync("/api/profile", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UserProfileResponseDto>(TestHelper.JsonOptions);
        result!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task ChangePassword_WithValidData_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "profpwd");

        var dto = new ChangePasswordDto
        {
            CurrentPassword = "Customer123!@#",
            NewPassword = "NewPass123!@#",
            ConfirmPassword = "NewPass123!@#"
        };

        var response = await client.PostAsJsonAsync("/api/profile/change-password", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "profpwdbad");

        var dto = new ChangePasswordDto
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPass123!@#",
            ConfirmPassword = "NewPass123!@#"
        };

        var response = await client.PostAsJsonAsync("/api/profile/change-password", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStats_WithAuth_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "profstats");

        var response = await client.GetAsync("/api/profile/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UserStatsDto>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNotificationPreferences_WithAuth_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "profnotif");

        var response = await client.GetAsync("/api/profile/notification-preferences");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateNotificationPreferences_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "profnotifupd");

        var dto = new UpdateNotificationPreferencesDto
        {
            EmailOrderUpdates = true,
            EmailPromotions = false,
            PushOrderUpdates = true
        };

        var response = await client.PutAsJsonAsync("/api/profile/notification-preferences", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProfileAddresses_CRUD_WorksCorrectly()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "profaddr");

        // Create address via profile endpoint
        var createDto = new CreateAddressDto
        {
            Title = "Profile Home",
            FullName = "Profile User",
            Phone = "+905551234567",
            City = "Ankara",
            District = "Cankaya",
            AddressLine = "Ataturk Blvd. No:1",
            Type = Exodus.Models.Entities.AddressType.Shipping
        };
        var createResp = await client.PostAsJsonAsync("/api/profile/addresses", createDto, TestHelper.JsonOptions);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<AddressResponseDto>(TestHelper.JsonOptions);
        created.Should().NotBeNull();

        // Get all addresses
        var listResp = await client.GetAsync("/api/profile/addresses");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Get by id
        var getResp = await client.GetAsync($"/api/profile/addresses/{created!.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Update
        var updateDto = new UpdateAddressDto { Title = "Updated Home" };
        var updateResp = await client.PutAsJsonAsync($"/api/profile/addresses/{created.Id}", updateDto, TestHelper.JsonOptions);
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Set default
        var defaultResp = await client.PatchAsync($"/api/profile/addresses/{created.Id}/default", null);
        defaultResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Delete
        var deleteResp = await client.DeleteAsync($"/api/profile/addresses/{created.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

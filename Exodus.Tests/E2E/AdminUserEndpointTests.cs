using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Exodus.Controllers.Admin;
using Exodus.Models.Enums;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class AdminUserEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminUserEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ==========================================
    // AUTHORIZATION TESTS
    // ==========================================

    [Fact]
    public async Task GetAllUsers_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllUsers_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "admusrcust");

        var response = await client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllUsers_AsSeller_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "admusrsell");

        var response = await client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ==========================================
    // GET ALL USERS
    // ==========================================

    [Fact]
    public async Task GetAllUsers_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admusrall");

        var response = await client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestHelper.JsonOptions);
        result.TryGetProperty("items", out _).Should().BeTrue();
        result.TryGetProperty("totalCount", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetAllUsers_WithSearch_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admusrsch");

        var response = await client.GetAsync("/api/admin/users?search=admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllUsers_WithRoleFilter_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admusrfilt");

        var response = await client.GetAsync("/api/admin/users?role=Customer");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllUsers_WithPagination_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admusrpage");

        var response = await client.GetAsync("/api/admin/users?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestHelper.JsonOptions);
        result.GetProperty("page").GetInt32().Should().Be(1);
        result.GetProperty("pageSize").GetInt32().Should().Be(5);
    }

    // ==========================================
    // GET USER BY ID
    // ==========================================

    [Fact]
    public async Task GetUserById_AsAdmin_ExistingUser_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admusrbyid");

        // Register a target customer
        var customer = await TestHelper.RegisterUserAsync(client,
            name: "Target Customer",
            email: "targetcust@example.com",
            username: "targetcust",
            role: UserRole.Customer);

        // Admin logs back in
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admusrbyid2");
        var response = await client.GetAsync($"/api/admin/users/{customer.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestHelper.JsonOptions);
        result.GetProperty("id").GetInt32().Should().Be(customer.UserId);
    }

    [Fact]
    public async Task GetUserById_AsAdmin_NonExistentUser_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admusrnotfound");

        var response = await client.GetAsync("/api/admin/users/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ==========================================
    // UPDATE USER
    // ==========================================

    [Fact]
    public async Task UpdateUser_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admupdatein");

        var customer = await TestHelper.RegisterUserAsync(client,
            name: "Update Target",
            email: "updatetarget@example.com",
            username: "updatetarget",
            role: UserRole.Customer);

        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admupdateout");

        var dto = new AdminUpdateUserDto { Name = "Updated Name" };
        var response = await client.PutAsJsonAsync($"/api/admin/users/{customer.UserId}", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateUser_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new AdminUpdateUserDto { Name = "Updated" };
        var response = await client.PutAsJsonAsync("/api/admin/users/1", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // USER STATS
    // ==========================================

    [Fact]
    public async Task GetStats_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admusrstats");

        var response = await client.GetAsync("/api/admin/users/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestHelper.JsonOptions);
        result.TryGetProperty("totalUsers", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetStats_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/users/stats");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // LOCK / UNLOCK USER
    // ==========================================

    [Fact]
    public async Task LockUser_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admlocksetup");

        var customer = await TestHelper.RegisterUserAsync(client,
            name: "Lock Target",
            email: "locktarget@example.com",
            username: "locktarget",
            role: UserRole.Customer);

        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admlockadmin");

        var dto = new LockAccountDto { DurationMinutes = 60 };
        var response = await client.PostAsJsonAsync($"/api/admin/users/{customer.UserId}/lock", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnlockUser_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admlocksetup2");

        var customer = await TestHelper.RegisterUserAsync(client,
            name: "Unlock Target",
            email: "unlocktarget@example.com",
            username: "unlocktarget",
            role: UserRole.Customer);

        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admlockadmin2");

        // First lock
        var lockDto = new LockAccountDto { DurationMinutes = 60 };
        await client.PostAsJsonAsync($"/api/admin/users/{customer.UserId}/lock", lockDto, TestHelper.JsonOptions);

        // Then unlock
        var response = await client.PostAsync($"/api/admin/users/{customer.UserId}/unlock", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LockUser_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new LockAccountDto { DurationMinutes = 30 };
        var response = await client.PostAsJsonAsync("/api/admin/users/1/lock", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // RESET PASSWORD
    // ==========================================

    [Fact]
    public async Task ResetPassword_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admresetsetup");

        var customer = await TestHelper.RegisterUserAsync(client,
            name: "Reset Target",
            email: "resettarget@example.com",
            username: "resettarget",
            role: UserRole.Customer);

        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admresetadmin");

        var response = await client.PostAsync($"/api/admin/users/{customer.UserId}/reset-password", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestHelper.JsonOptions);
        result.TryGetProperty("temporaryPassword", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ResetPassword_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/admin/users/1/reset-password", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // CHANGE ROLE
    // ==========================================

    [Fact]
    public async Task ChangeRole_AsAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admrolesetup");

        var customer = await TestHelper.RegisterUserAsync(client,
            name: "Role Target",
            email: "roletarget@example.com",
            username: "roletarget",
            role: UserRole.Customer);

        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admroleadmin");

        var dto = new ChangeRoleDto { Role = UserRole.Seller };
        var response = await client.PatchAsJsonAsync($"/api/admin/users/{customer.UserId}/role", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangeRole_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new ChangeRoleDto { Role = UserRole.Seller };
        var response = await client.PatchAsJsonAsync("/api/admin/users/1/role", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // DELETE USER
    // ==========================================

    [Fact]
    public async Task DeleteUser_AsAdmin_DifferentUser_ReturnsNoContent()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admdelsetup");

        var customer = await TestHelper.RegisterUserAsync(client,
            name: "Delete Target",
            email: "deletetarget@example.com",
            username: "deletetarget",
            role: UserRole.Customer);

        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admdeladmin");

        var response = await client.DeleteAsync($"/api/admin/users/{customer.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteUser_AsAdmin_SelfDelete_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var admin = await TestHelper.RegisterAndLoginAsAdminAsync(client, "admdelself");

        var response = await client.DeleteAsync($"/api/admin/users/{admin.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteUser_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/admin/users/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==========================================
    // TOGGLE ACTIVE
    // ==========================================

    [Fact]
    public async Task ToggleActive_AsAdmin_Customer_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admtogsetup");

        var customer = await TestHelper.RegisterUserAsync(client,
            name: "Toggle Target",
            email: "toggletarget@example.com",
            username: "toggletarget",
            role: UserRole.Customer);

        await TestHelper.RegisterAndLoginAsAdminAsync(client, "admtogadmin");

        var response = await client.PatchAsync($"/api/admin/users/{customer.UserId}/toggle-active", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestHelper.JsonOptions);
        result.TryGetProperty("isActive", out _).Should().BeTrue();
    }
}

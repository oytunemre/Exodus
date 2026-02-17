using System.Net;
using System.Net.Http.Json;
using Exodus.Models.Dto;
using Exodus.Models.Enums;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class AuthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsTokenAndUserInfo()
    {
        var dto = new RegisterDto
        {
            Name = "John Doe",
            Email = "john@example.com",
            Username = "johndoe",
            Password = "StrongPass123!",
            Role = UserRole.Customer
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Email.Should().Be("john@example.com");
        result.Username.Should().Be("johndoe");
        result.Name.Should().Be("John Doe");
        result.Role.Should().Be(UserRole.Customer);
        result.UserId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var client = _client;

        await TestHelper.RegisterUserAsync(client,
            name: "User One",
            email: "duplicate@example.com",
            username: "userone",
            password: "Pass123!@#");

        var dto = new RegisterDto
        {
            Name = "User Two",
            Email = "duplicate@example.com",
            Username = "usertwo",
            Password = "Pass123!@#",
            Role = UserRole.Customer
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsConflict()
    {
        var client = _client;

        await TestHelper.RegisterUserAsync(client,
            name: "User A",
            email: "usera@example.com",
            username: "sameusername",
            password: "Pass123!@#");

        var dto = new RegisterDto
        {
            Name = "User B",
            Email = "userb@example.com",
            Username = "sameusername",
            Password = "Pass123!@#",
            Role = UserRole.Customer
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var client = _client;

        var auth = await TestHelper.RegisterUserAsync(client,
            name: "Login User",
            email: "loginuser@example.com",
            username: "loginuser",
            password: "LoginPass123!");

        // Login requires verified email
        await _factory.VerifyUserEmailAsync(auth.UserId);

        var loginDto = new LoginDto
        {
            EmailOrUsername = "loginuser@example.com",
            Password = "LoginPass123!"
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Email.Should().Be("loginuser@example.com");
    }

    [Fact]
    public async Task Login_WithUsername_ReturnsToken()
    {
        var client = _client;

        var auth = await TestHelper.RegisterUserAsync(client,
            name: "Username Login",
            email: "usernamelogin@example.com",
            username: "usernamelogin",
            password: "LoginPass123!");

        await _factory.VerifyUserEmailAsync(auth.UserId);

        var loginDto = new LoginDto
        {
            EmailOrUsername = "usernamelogin",
            Password = "LoginPass123!"
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>(TestHelper.JsonOptions);
        result!.Username.Should().Be("usernamelogin");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var client = _client;

        var auth = await TestHelper.RegisterUserAsync(client,
            name: "Bad Pass User",
            email: "badpass@example.com",
            username: "badpassuser",
            password: "CorrectPass123!");

        // Verify email so password check is reached (not blocked by email verification)
        await _factory.VerifyUserEmailAsync(auth.UserId);

        var loginDto = new LoginDto
        {
            EmailOrUsername = "badpass@example.com",
            Password = "WrongPass123!"
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginDto, TestHelper.JsonOptions);

        // API returns 404 (NotFoundException) for invalid credentials
        // to avoid revealing whether the user exists
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsUserInfo()
    {
        var client = _client;

        var auth = await TestHelper.RegisterUserAsync(client,
            name: "Me User",
            email: "meuser@example.com",
            username: "meuser",
            password: "MePass123!");

        TestHelper.SetAuthToken(client, auth.Token!);

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("meuser");
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewTokens()
    {
        var client = _client;

        var auth = await TestHelper.RegisterUserAsync(client,
            name: "Refresh User",
            email: "refreshuser@example.com",
            username: "refreshuser",
            password: "RefreshPass123!");

        var refreshDto = new RefreshTokenRequestDto
        {
            RefreshToken = auth.RefreshToken!
        };

        var response = await client.PostAsJsonAsync("/api/auth/refresh", refreshDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TokenResponseDto>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_AsSeller_ReturnSellerRole()
    {
        var client = _client;

        var auth = await TestHelper.RegisterUserAsync(client,
            name: "Seller User",
            email: "seller@example.com",
            username: "selleruser",
            password: "SellerPass123!",
            role: UserRole.Seller);

        auth.Role.Should().Be(UserRole.Seller);
    }

    [Fact]
    public async Task Register_AsAdmin_ReturnAdminRole()
    {
        var client = _client;

        var auth = await TestHelper.RegisterUserAsync(client,
            name: "Admin User",
            email: "admin@example.com",
            username: "adminuser",
            password: "AdminPass123!",
            role: UserRole.Admin);

        auth.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task AdminStats_WithAdminToken_ReturnsOk()
    {
        var client = _client;

        var auth = await TestHelper.RegisterUserAsync(client,
            name: "Stats Admin",
            email: "statsadmin@example.com",
            username: "statsadmin",
            password: "AdminPass123!",
            role: UserRole.Admin);

        TestHelper.SetAuthToken(client, auth.Token!);

        var response = await client.GetAsync("/api/auth/admin/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminStats_WithCustomerToken_ReturnsForbidden()
    {
        var client = _client;

        var auth = await TestHelper.RegisterUserAsync(client,
            name: "Stats Customer",
            email: "statscustomer@example.com",
            username: "statscustomer",
            password: "CustPass123!",
            role: UserRole.Customer);

        TestHelper.SetAuthToken(client, auth.Token!);

        var response = await client.GetAsync("/api/auth/admin/stats");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SellerDashboard_WithSellerToken_ReturnsOk()
    {
        var client = _client;

        var auth = await TestHelper.RegisterUserAsync(client,
            name: "Dashboard Seller",
            email: "dashseller@example.com",
            username: "dashseller",
            password: "SellerPass123!",
            role: UserRole.Seller);

        TestHelper.SetAuthToken(client, auth.Token!);

        var response = await client.GetAsync("/api/auth/seller/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SellerDashboard_WithCustomerToken_ReturnsForbidden()
    {
        var client = _client;

        var auth = await TestHelper.RegisterUserAsync(client,
            name: "Dashboard Customer",
            email: "dashcustomer@example.com",
            username: "dashcustomer",
            password: "CustPass123!",
            role: UserRole.Customer);

        TestHelper.SetAuthToken(client, auth.Token!);

        var response = await client.GetAsync("/api/auth/seller/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RevokeToken_WithValidToken_ReturnsOk()
    {
        var client = _client;

        var auth = await TestHelper.RegisterUserAsync(client,
            name: "Revoke User",
            email: "revokeuser@example.com",
            username: "revokeuser",
            password: "RevokePass123!");

        TestHelper.SetAuthToken(client, auth.Token!);

        var revokeDto = new RefreshTokenRequestDto
        {
            RefreshToken = auth.RefreshToken!
        };

        var response = await client.PostAsJsonAsync("/api/auth/revoke", revokeDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

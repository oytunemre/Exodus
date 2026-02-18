using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Exodus.Models.Dto;
using Exodus.Models.Enums;

namespace Exodus.Tests.E2E;

public static class TestHelper
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<AuthResponseDto> RegisterUserAsync(
        HttpClient client,
        string name = "Test User",
        string email = "test@example.com",
        string username = "testuser",
        string password = "Test123!@#",
        UserRole role = UserRole.Customer)
    {
        var dto = new RegisterDto
        {
            Name = name,
            Email = email,
            Username = username,
            Password = password,
            Role = role
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        return result!;
    }

    public static async Task<AuthResponseDto> LoginAsync(
        HttpClient client,
        string emailOrUsername,
        string password = "Test123!@#")
    {
        var dto = new LoginDto
        {
            EmailOrUsername = emailOrUsername,
            Password = password
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        return result!;
    }

    public static void SetAuthToken(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task<AuthResponseDto> RegisterAndLoginAsAdminAsync(
        HttpClient client,
        string suffix = "")
    {
        var auth = await RegisterUserAsync(client,
            name: "Admin User" + suffix,
            email: $"admin{suffix}@example.com",
            username: $"adminuser{suffix}",
            password: "Admin123!@#",
            role: UserRole.Admin);
        SetAuthToken(client, auth.Token!);
        return auth;
    }

    public static async Task<AuthResponseDto> RegisterAndLoginAsSellerAsync(
        HttpClient client,
        string suffix = "")
    {
        var auth = await RegisterUserAsync(client,
            name: "Seller User" + suffix,
            email: $"seller{suffix}@example.com",
            username: $"selleruser{suffix}",
            password: "Seller123!@#",
            role: UserRole.Seller);
        SetAuthToken(client, auth.Token!);
        return auth;
    }

    public static async Task<AuthResponseDto> RegisterAndLoginAsCustomerAsync(
        HttpClient client,
        string suffix = "")
    {
        var auth = await RegisterUserAsync(client,
            name: "Customer User" + suffix,
            email: $"customer{suffix}@example.com",
            username: $"customeruser{suffix}",
            password: "Customer123!@#",
            role: UserRole.Customer);
        SetAuthToken(client, auth.Token!);
        return auth;
    }
}

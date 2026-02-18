using System.Net;
using System.Net.Http.Json;
using Exodus.Models.Dto;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class NotificationEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NotificationEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetNotifications_WithAuth_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "notiflist");

        var response = await client.GetAsync("/api/notification");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<NotificationListDto>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetNotifications_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/notification");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNotifications_WithPagination_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "notifpage");

        var response = await client.GetAsync("/api/notification?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<NotificationListDto>(TestHelper.JsonOptions);
        result!.PageSize.Should().Be(5);
    }

    [Fact]
    public async Task GetUnreadCount_WithAuth_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "notifcount");

        var response = await client.GetAsync("/api/notification/unread-count");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkAllAsRead_WithAuth_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "notifreadall");

        var response = await client.PatchAsync("/api/notification/read-all", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteReadNotifications_WithAuth_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "notifdelread");

        var response = await client.DeleteAsync("/api/notification/read");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

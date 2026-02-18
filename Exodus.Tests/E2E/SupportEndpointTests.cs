using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class SupportEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SupportEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetTickets_WithAuth_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "supplist");

        var response = await client.GetAsync("/api/support/tickets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTickets_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/support/tickets");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTicket_WithValidData_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "suppcreate");

        var dto = new
        {
            Subject = "Test Support Ticket",
            Message = "I need help with my order",
            Category = "General",
            Priority = "Normal"
        };

        var response = await client.PostAsJsonAsync("/api/support/tickets", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateTicket_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new
        {
            Subject = "Unauthorized Ticket",
            Message = "Should not work"
        };

        var response = await client.PostAsJsonAsync("/api/support/tickets", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTicketById_AfterCreate_ReturnsTicket()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "suppgetid");

        var dto = new
        {
            Subject = "Ticket To Fetch",
            Message = "Testing fetch by ID",
            Category = "General"
        };

        var createResp = await client.PostAsJsonAsync("/api/support/tickets", dto, TestHelper.JsonOptions);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await createResp.Content.ReadAsStringAsync();
        var created = JsonDocument.Parse(content).RootElement;
        var ticketId = created.GetProperty("ticketId").GetInt32();

        var response = await client.GetAsync($"/api/support/tickets/{ticketId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReplyToTicket_WithValidData_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "suppreply");

        // Create ticket
        var createDto = new
        {
            Subject = "Ticket For Reply",
            Message = "Initial message",
            Category = "General"
        };
        var createResp = await client.PostAsJsonAsync("/api/support/tickets", createDto, TestHelper.JsonOptions);
        var content = await createResp.Content.ReadAsStringAsync();
        var created = JsonDocument.Parse(content).RootElement;
        var ticketId = created.GetProperty("ticketId").GetInt32();

        // Reply
        var replyDto = new { Message = "Follow-up message" };
        var response = await client.PostAsJsonAsync($"/api/support/tickets/{ticketId}/messages", replyDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CloseTicket_AfterCreate_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "suppclose");

        // Create ticket
        var createDto = new
        {
            Subject = "Ticket To Close",
            Message = "Will be closed",
            Category = "General"
        };
        var createResp = await client.PostAsJsonAsync("/api/support/tickets", createDto, TestHelper.JsonOptions);
        var content = await createResp.Content.ReadAsStringAsync();
        var created = JsonDocument.Parse(content).RootElement;
        var ticketId = created.GetProperty("ticketId").GetInt32();

        // Close
        var closeDto = new { SatisfactionRating = 5, SatisfactionComment = "Great support!" };
        var response = await client.PostAsJsonAsync($"/api/support/tickets/{ticketId}/close", closeDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTickets_WithPagination_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "supppage");

        var response = await client.GetAsync("/api/support/tickets?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

using System.Net;
using System.Net.Http.Json;
using Exodus.Models.Dto.ProductDto;
using Exodus.Services.ProductQA;
using FluentAssertions;
using Xunit;

namespace Exodus.Tests.E2E;

public class ProductQAEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProductQAEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<int> CreateTestProductAsync(HttpClient client, string suffix)
    {
        await TestHelper.RegisterAndLoginAsSellerAsync(client, suffix);
        var dto = new AddProductDto
        {
            ProductName = "QA Test Product " + suffix,
            ProductDescription = "Product for Q&A tests",
            Barcodes = new List<string> { Guid.NewGuid().ToString("N") }
        };
        var resp = await client.PostAsJsonAsync("/api/product", dto, TestHelper.JsonOptions);
        var product = await resp.Content.ReadFromJsonAsync<ProductResponseDto>(TestHelper.JsonOptions);
        return product!.Id;
    }

    [Fact]
    public async Task GetQuestions_ForProduct_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var productId = await CreateTestProductAsync(client, "qagetq");

        // Questions endpoint is AllowAnonymous
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.GetAsync($"/api/products/{productId}/questions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AskQuestion_WithAuth_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        var productId = await CreateTestProductAsync(client, "qaask");

        // Switch to customer
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "qaask");

        var dto = new AskQuestionDto
        {
            ProductId = productId,
            QuestionText = "Is this product waterproof?"
        };

        var response = await client.PostAsJsonAsync($"/api/products/{productId}/questions", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<QuestionResponseDto>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
        result!.QuestionText.Should().Be("Is this product waterproof?");
    }

    [Fact]
    public async Task AskQuestion_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new AskQuestionDto
        {
            ProductId = 1,
            QuestionText = "Unauthorized question"
        };

        var response = await client.PostAsJsonAsync("/api/products/1/questions", dto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnswerQuestion_WithAuth_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        var productId = await CreateTestProductAsync(client, "qaanswer");

        // Ask a question as customer
        var customer = await TestHelper.RegisterAndLoginAsCustomerAsync(client, "qaanswer");
        var askDto = new AskQuestionDto
        {
            ProductId = productId,
            QuestionText = "What is the warranty?"
        };
        var askResp = await client.PostAsJsonAsync($"/api/products/{productId}/questions", askDto, TestHelper.JsonOptions);
        var question = await askResp.Content.ReadFromJsonAsync<QuestionResponseDto>(TestHelper.JsonOptions);

        // Answer as seller (re-login)
        await TestHelper.RegisterAndLoginAsSellerAsync(client, "qaanswer2");

        var answerDto = new AnswerQuestionDto
        {
            AnswerText = "2 years manufacturer warranty."
        };

        var response = await client.PostAsJsonAsync(
            $"/api/products/{productId}/questions/{question!.Id}/answers",
            answerDto, TestHelper.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<AnswerResponseDto>(TestHelper.JsonOptions);
        result.Should().NotBeNull();
        result!.AnswerText.Should().Be("2 years manufacturer warranty.");
    }

    [Fact]
    public async Task GetQuestionById_ReturnsQuestion()
    {
        var client = _factory.CreateClient();
        var productId = await CreateTestProductAsync(client, "qagetbyid");

        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "qagetbyid");
        var askDto = new AskQuestionDto
        {
            ProductId = productId,
            QuestionText = "Question to fetch"
        };
        var askResp = await client.PostAsJsonAsync($"/api/products/{productId}/questions", askDto, TestHelper.JsonOptions);
        var question = await askResp.Content.ReadFromJsonAsync<QuestionResponseDto>(TestHelper.JsonOptions);

        // Public endpoint
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.GetAsync($"/api/products/{productId}/questions/{question!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpvoteQuestion_WithAuth_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var productId = await CreateTestProductAsync(client, "qaupvote");

        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "qaupvote");
        var askDto = new AskQuestionDto
        {
            ProductId = productId,
            QuestionText = "Good question to upvote"
        };
        var askResp = await client.PostAsJsonAsync($"/api/products/{productId}/questions", askDto, TestHelper.JsonOptions);
        var question = await askResp.Content.ReadFromJsonAsync<QuestionResponseDto>(TestHelper.JsonOptions);

        // Upvote as a different user
        await TestHelper.RegisterAndLoginAsCustomerAsync(client, "qaupvoter");
        var response = await client.PostAsync(
            $"/api/products/{productId}/questions/{question!.Id}/upvote", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

using Exodus.Models.Entities;
using Exodus.Services.ProductQA;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Exodus.Controllers;

[ApiController]
[Route("api/products")]
public class ProductQAController : ControllerBase
{
    private readonly IProductQAService _qaService;

    public ProductQAController(IProductQAService qaService)
    {
        _qaService = qaService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Urun hakkinda soru sor
    /// </summary>
    [HttpPost("{productId}/questions")]
    [Authorize]
    public async Task<IActionResult> AskQuestion(int productId, [FromBody] AskQuestionDto dto, CancellationToken ct)
    {
        dto.ProductId = productId;
        var question = await _qaService.AskQuestionAsync(GetUserId(), dto, ct);
        return CreatedAtAction(nameof(GetQuestion), new { productId, questionId = question.Id }, question);
    }

    /// <summary>
    /// Urunun sorularini listele
    /// </summary>
    [HttpGet("{productId}/questions")]
    [AllowAnonymous]
    public async Task<IActionResult> GetQuestions(int productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var questions = await _qaService.GetQuestionsByProductAsync(productId, page, pageSize, ct);
        return Ok(questions);
    }

    /// <summary>
    /// Belirli bir soruyu getir
    /// </summary>
    [HttpGet("{productId}/questions/{questionId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetQuestion(int productId, int questionId, CancellationToken ct)
    {
        var question = await _qaService.GetQuestionByIdAsync(questionId, ct);
        return Ok(question);
    }

    /// <summary>
    /// Soruyu sil (sadece soran kisi)
    /// </summary>
    [HttpDelete("{productId}/questions/{questionId}")]
    [Authorize]
    public async Task<IActionResult> DeleteQuestion(int productId, int questionId, CancellationToken ct)
    {
        await _qaService.DeleteQuestionAsync(GetUserId(), questionId, ct);
        return NoContent();
    }

    /// <summary>
    /// Soruyu upvote et
    /// </summary>
    [HttpPost("{productId}/questions/{questionId}/upvote")]
    [Authorize]
    public async Task<IActionResult> UpvoteQuestion(int productId, int questionId, CancellationToken ct)
    {
        var question = await _qaService.UpvoteQuestionAsync(GetUserId(), questionId, ct);
        return Ok(question);
    }

    /// <summary>
    /// Soruya cevap ver
    /// </summary>
    [HttpPost("{productId}/questions/{questionId}/answers")]
    [Authorize]
    public async Task<IActionResult> AnswerQuestion(int productId, int questionId, [FromBody] AnswerQuestionDto dto, CancellationToken ct)
    {
        var answer = await _qaService.AnswerQuestionAsync(GetUserId(), questionId, dto, ct);
        return CreatedAtAction(nameof(GetQuestion), new { productId, questionId }, answer);
    }

    /// <summary>
    /// Cevabi kabul et (sadece soruyu soran)
    /// </summary>
    [HttpPatch("{productId}/questions/{questionId}/answers/{answerId}/accept")]
    [Authorize]
    public async Task<IActionResult> AcceptAnswer(int productId, int questionId, int answerId, CancellationToken ct)
    {
        var answer = await _qaService.AcceptAnswerAsync(GetUserId(), answerId, ct);
        return Ok(answer);
    }

    /// <summary>
    /// Cevabi sil
    /// </summary>
    [HttpDelete("{productId}/questions/{questionId}/answers/{answerId}")]
    [Authorize]
    public async Task<IActionResult> DeleteAnswer(int productId, int questionId, int answerId, CancellationToken ct)
    {
        await _qaService.DeleteAnswerAsync(GetUserId(), answerId, ct);
        return NoContent();
    }

    /// <summary>
    /// Cevabi upvote et
    /// </summary>
    [HttpPost("{productId}/questions/{questionId}/answers/{answerId}/upvote")]
    [Authorize]
    public async Task<IActionResult> UpvoteAnswer(int productId, int questionId, int answerId, CancellationToken ct)
    {
        var answer = await _qaService.UpvoteAnswerAsync(GetUserId(), answerId, ct);
        return Ok(answer);
    }
}

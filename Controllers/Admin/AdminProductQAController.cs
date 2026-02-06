using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.ProductQA;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmazonDemo.Controllers.Admin;

[ApiController]
[Route("api/admin/product-qa")]
[Authorize(Policy = "AdminOnly")]
public class AdminProductQAController : ControllerBase
{
    private readonly IProductQAService _qaService;

    public AdminProductQAController(IProductQAService qaService)
    {
        _qaService = qaService;
    }

    /// <summary>
    /// Onay bekleyen sorulari listele
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingQuestions([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var questions = await _qaService.GetPendingQuestionsAsync(page, pageSize, ct);
        return Ok(questions);
    }

    /// <summary>
    /// Soruyu onayla veya reddet
    /// </summary>
    [HttpPatch("{questionId}/moderate")]
    public async Task<IActionResult> ModerateQuestion(int questionId, [FromBody] ModerateQuestionDto dto, CancellationToken ct)
    {
        if (!Enum.TryParse<QuestionStatus>(dto.Status, true, out var status))
            return BadRequest("Gecersiz durum. Gecerli degerler: Pending, Approved, Rejected, Answered");

        var question = await _qaService.ModerateQuestionAsync(questionId, status, ct);
        return Ok(question);
    }
}

public class ModerateQuestionDto
{
    public string Status { get; set; } = string.Empty;
}

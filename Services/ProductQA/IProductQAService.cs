using FarmazonDemo.Models.Entities;

namespace FarmazonDemo.Services.ProductQA;

public interface IProductQAService
{
    // Sorular
    Task<QuestionResponseDto> AskQuestionAsync(int userId, AskQuestionDto dto, CancellationToken ct = default);
    Task<List<QuestionResponseDto>> GetQuestionsByProductAsync(int productId, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<QuestionResponseDto> GetQuestionByIdAsync(int questionId, CancellationToken ct = default);
    Task DeleteQuestionAsync(int userId, int questionId, CancellationToken ct = default);
    Task<QuestionResponseDto> UpvoteQuestionAsync(int userId, int questionId, CancellationToken ct = default);

    // Cevaplar
    Task<AnswerResponseDto> AnswerQuestionAsync(int userId, int questionId, AnswerQuestionDto dto, CancellationToken ct = default);
    Task<AnswerResponseDto> AcceptAnswerAsync(int userId, int answerId, CancellationToken ct = default);
    Task DeleteAnswerAsync(int userId, int answerId, CancellationToken ct = default);
    Task<AnswerResponseDto> UpvoteAnswerAsync(int userId, int answerId, CancellationToken ct = default);

    // Admin
    Task<QuestionResponseDto> ModerateQuestionAsync(int questionId, QuestionStatus status, CancellationToken ct = default);
    Task<List<QuestionResponseDto>> GetPendingQuestionsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
}

public class AskQuestionDto
{
    public int ProductId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
}

public class AnswerQuestionDto
{
    public string AnswerText { get; set; } = string.Empty;
}

public class QuestionResponseDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string AskedByUserName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int UpvoteCount { get; set; }
    public int AnswerCount { get; set; }
    public List<AnswerResponseDto> Answers { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class AnswerResponseDto
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public string AnsweredByUserName { get; set; } = string.Empty;
    public bool IsSellerAnswer { get; set; }
    public bool IsAccepted { get; set; }
    public int UpvoteCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

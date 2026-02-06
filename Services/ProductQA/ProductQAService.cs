using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Services.ProductQA;

public class ProductQAService : IProductQAService
{
    private readonly ApplicationDbContext _db;

    public ProductQAService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<QuestionResponseDto> AskQuestionAsync(int userId, AskQuestionDto dto, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync(new object[] { dto.ProductId }, ct)
            ?? throw new KeyNotFoundException("Urun bulunamadi");

        var question = new ProductQuestion
        {
            ProductId = dto.ProductId,
            AskedByUserId = userId,
            QuestionText = dto.QuestionText,
            Status = QuestionStatus.Pending
        };

        _db.Set<ProductQuestion>().Add(question);
        await _db.SaveChangesAsync(ct);

        return await GetQuestionByIdAsync(question.Id, ct);
    }

    public async Task<List<QuestionResponseDto>> GetQuestionsByProductAsync(int productId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var questions = await _db.Set<ProductQuestion>()
            .Include(q => q.AskedByUser)
            .Include(q => q.Product)
            .Include(q => q.Answers).ThenInclude(a => a.AnsweredByUser)
            .Where(q => q.ProductId == productId && q.Status != QuestionStatus.Rejected)
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return questions.Select(MapQuestionToDto).ToList();
    }

    public async Task<QuestionResponseDto> GetQuestionByIdAsync(int questionId, CancellationToken ct = default)
    {
        var question = await _db.Set<ProductQuestion>()
            .Include(q => q.AskedByUser)
            .Include(q => q.Product)
            .Include(q => q.Answers).ThenInclude(a => a.AnsweredByUser)
            .FirstOrDefaultAsync(q => q.Id == questionId, ct)
            ?? throw new KeyNotFoundException("Soru bulunamadi");

        return MapQuestionToDto(question);
    }

    public async Task DeleteQuestionAsync(int userId, int questionId, CancellationToken ct = default)
    {
        var question = await _db.Set<ProductQuestion>()
            .FirstOrDefaultAsync(q => q.Id == questionId && q.AskedByUserId == userId, ct)
            ?? throw new KeyNotFoundException("Soru bulunamadi");

        _db.Set<ProductQuestion>().Remove(question);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<QuestionResponseDto> UpvoteQuestionAsync(int userId, int questionId, CancellationToken ct = default)
    {
        var question = await _db.Set<ProductQuestion>()
            .FirstOrDefaultAsync(q => q.Id == questionId, ct)
            ?? throw new KeyNotFoundException("Soru bulunamadi");

        question.UpvoteCount++;
        await _db.SaveChangesAsync(ct);

        return await GetQuestionByIdAsync(questionId, ct);
    }

    public async Task<AnswerResponseDto> AnswerQuestionAsync(int userId, int questionId, AnswerQuestionDto dto, CancellationToken ct = default)
    {
        var question = await _db.Set<ProductQuestion>()
            .Include(q => q.Product)
            .FirstOrDefaultAsync(q => q.Id == questionId, ct)
            ?? throw new KeyNotFoundException("Soru bulunamadi");

        // Satici mi kontrol et
        var listing = await _db.Listings
            .FirstOrDefaultAsync(l => l.ProductId == question.ProductId && l.SellerId == userId, ct);

        var answer = new ProductAnswer
        {
            QuestionId = questionId,
            AnsweredByUserId = userId,
            AnswerText = dto.AnswerText,
            IsSellerAnswer = listing != null
        };

        _db.Set<ProductAnswer>().Add(answer);

        if (question.Status == QuestionStatus.Pending)
            question.Status = QuestionStatus.Answered;

        await _db.SaveChangesAsync(ct);

        return MapAnswerToDto(answer, await _db.Users.FindAsync(new object[] { userId }, ct));
    }

    public async Task<AnswerResponseDto> AcceptAnswerAsync(int userId, int answerId, CancellationToken ct = default)
    {
        var answer = await _db.Set<ProductAnswer>()
            .Include(a => a.Question)
            .Include(a => a.AnsweredByUser)
            .FirstOrDefaultAsync(a => a.Id == answerId, ct)
            ?? throw new KeyNotFoundException("Cevap bulunamadi");

        if (answer.Question.AskedByUserId != userId)
            throw new UnauthorizedAccessException("Sadece soruyu soran cevabi kabul edebilir");

        answer.IsAccepted = true;
        await _db.SaveChangesAsync(ct);

        return MapAnswerToDto(answer, answer.AnsweredByUser);
    }

    public async Task DeleteAnswerAsync(int userId, int answerId, CancellationToken ct = default)
    {
        var answer = await _db.Set<ProductAnswer>()
            .FirstOrDefaultAsync(a => a.Id == answerId && a.AnsweredByUserId == userId, ct)
            ?? throw new KeyNotFoundException("Cevap bulunamadi");

        _db.Set<ProductAnswer>().Remove(answer);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AnswerResponseDto> UpvoteAnswerAsync(int userId, int answerId, CancellationToken ct = default)
    {
        var answer = await _db.Set<ProductAnswer>()
            .Include(a => a.AnsweredByUser)
            .FirstOrDefaultAsync(a => a.Id == answerId, ct)
            ?? throw new KeyNotFoundException("Cevap bulunamadi");

        answer.UpvoteCount++;
        await _db.SaveChangesAsync(ct);

        return MapAnswerToDto(answer, answer.AnsweredByUser);
    }

    public async Task<QuestionResponseDto> ModerateQuestionAsync(int questionId, QuestionStatus status, CancellationToken ct = default)
    {
        var question = await _db.Set<ProductQuestion>()
            .FirstOrDefaultAsync(q => q.Id == questionId, ct)
            ?? throw new KeyNotFoundException("Soru bulunamadi");

        question.Status = status;
        await _db.SaveChangesAsync(ct);

        return await GetQuestionByIdAsync(questionId, ct);
    }

    public async Task<List<QuestionResponseDto>> GetPendingQuestionsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var questions = await _db.Set<ProductQuestion>()
            .Include(q => q.AskedByUser)
            .Include(q => q.Product)
            .Include(q => q.Answers).ThenInclude(a => a.AnsweredByUser)
            .Where(q => q.Status == QuestionStatus.Pending)
            .OrderBy(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return questions.Select(MapQuestionToDto).ToList();
    }

    private static QuestionResponseDto MapQuestionToDto(ProductQuestion q)
    {
        return new QuestionResponseDto
        {
            Id = q.Id,
            ProductId = q.ProductId,
            ProductName = q.Product?.ProductName ?? string.Empty,
            QuestionText = q.QuestionText,
            AskedByUserName = q.AskedByUser?.Name ?? string.Empty,
            Status = q.Status.ToString(),
            UpvoteCount = q.UpvoteCount,
            AnswerCount = q.Answers?.Count ?? 0,
            Answers = q.Answers?.Select(a => MapAnswerToDto(a, a.AnsweredByUser)).ToList() ?? new(),
            CreatedAt = q.CreatedAt
        };
    }

    private static AnswerResponseDto MapAnswerToDto(ProductAnswer a, Users? user)
    {
        return new AnswerResponseDto
        {
            Id = a.Id,
            QuestionId = a.QuestionId,
            AnswerText = a.AnswerText,
            AnsweredByUserName = user?.Name ?? string.Empty,
            IsSellerAnswer = a.IsSellerAnswer,
            IsAccepted = a.IsAccepted,
            UpvoteCount = a.UpvoteCount,
            CreatedAt = a.CreatedAt
        };
    }
}

using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FarmazonDemo.Controllers.Admin;

[Route("api/admin/gift-cards")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminGiftCardController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminGiftCardController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> GetGiftCards(
        [FromQuery] GiftCardStatus? status = null,
        [FromQuery] string? code = null,
        [FromQuery] int? purchasedByUserId = null,
        [FromQuery] int? recipientUserId = null,
        [FromQuery] bool? hasBalance = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] bool sortDesc = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Set<GiftCard>()
            .Include(g => g.PurchasedBy)
            .Include(g => g.Recipient)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(g => g.Status == status.Value);

        if (!string.IsNullOrEmpty(code))
            query = query.Where(g => g.Code.Contains(code));

        if (purchasedByUserId.HasValue)
            query = query.Where(g => g.PurchasedByUserId == purchasedByUserId.Value);

        if (recipientUserId.HasValue)
            query = query.Where(g => g.RecipientUserId == recipientUserId.Value);

        if (hasBalance == true)
            query = query.Where(g => g.CurrentBalance > 0);
        else if (hasBalance == false)
            query = query.Where(g => g.CurrentBalance == 0);

        query = sortBy?.ToLower() switch
        {
            "code" => sortDesc ? query.OrderByDescending(g => g.Code) : query.OrderBy(g => g.Code),
            "balance" => sortDesc ? query.OrderByDescending(g => g.CurrentBalance) : query.OrderBy(g => g.CurrentBalance),
            "status" => sortDesc ? query.OrderByDescending(g => g.Status) : query.OrderBy(g => g.Status),
            _ => sortDesc ? query.OrderByDescending(g => g.CreatedAt) : query.OrderBy(g => g.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var giftCards = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new
            {
                g.Id,
                g.Code,
                g.InitialBalance,
                g.CurrentBalance,
                g.Currency,
                g.Status,
                StatusName = g.Status.ToString(),
                g.ExpiresAt,
                IsExpired = g.ExpiresAt.HasValue && g.ExpiresAt < DateTime.UtcNow,
                g.PurchasedByUserId,
                PurchasedByEmail = g.PurchasedBy != null ? g.PurchasedBy.Email : null,
                g.RecipientUserId,
                RecipientEmail = g.Recipient != null ? g.Recipient.Email : g.RecipientEmail,
                g.RecipientName,
                g.IsSentToRecipient,
                g.SentAt,
                g.RedeemedAt,
                g.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Items = giftCards,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetGiftCard(int id)
    {
        var giftCard = await _db.Set<GiftCard>()
            .Include(g => g.PurchasedBy)
            .Include(g => g.Recipient)
            .Include(g => g.Usages)
                .ThenInclude(u => u.User)
            .Include(g => g.Order)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (giftCard == null) throw new NotFoundException("Gift card not found");

        return Ok(new
        {
            giftCard.Id,
            giftCard.Code,
            giftCard.InitialBalance,
            giftCard.CurrentBalance,
            giftCard.Currency,
            giftCard.Status,
            StatusName = giftCard.Status.ToString(),
            giftCard.ExpiresAt,
            IsExpired = giftCard.ExpiresAt.HasValue && giftCard.ExpiresAt < DateTime.UtcNow,
            giftCard.PurchasedByUserId,
            PurchasedBy = giftCard.PurchasedBy != null ? new
            {
                giftCard.PurchasedBy.Id,
                giftCard.PurchasedBy.Email,
                Name = giftCard.PurchasedBy.Name
            } : null,
            giftCard.OrderId,
            giftCard.RecipientUserId,
            Recipient = giftCard.Recipient != null ? new
            {
                giftCard.Recipient.Id,
                giftCard.Recipient.Email,
                Name = giftCard.Recipient.Name
            } : null,
            giftCard.RecipientEmail,
            giftCard.RecipientName,
            giftCard.PersonalMessage,
            giftCard.IsSentToRecipient,
            giftCard.SentAt,
            giftCard.RedeemedAt,
            giftCard.RedeemedByUserId,
            giftCard.AdminNotes,
            Usages = giftCard.Usages.OrderByDescending(u => u.CreatedAt).Select(u => new
            {
                u.Id,
                u.OrderId,
                u.UserId,
                UserEmail = u.User.Email,
                u.Amount,
                u.BalanceAfter,
                u.Type,
                TypeName = u.Type.ToString(),
                u.Description,
                u.CreatedAt
            }),
            giftCard.CreatedAt,
            giftCard.UpdatedAt
        });
    }

    [HttpGet("code/{code}")]
    public async Task<ActionResult> GetGiftCardByCode(string code)
    {
        var giftCard = await _db.Set<GiftCard>().FirstOrDefaultAsync(g => g.Code == code);
        if (giftCard == null) throw new NotFoundException("Gift card not found");
        return await GetGiftCard(giftCard.Id);
    }

    [HttpPost]
    public async Task<ActionResult> CreateGiftCard([FromBody] CreateGiftCardDto dto)
    {
        var code = string.IsNullOrEmpty(dto.Code) ? GenerateGiftCardCode() : dto.Code;

        if (await _db.Set<GiftCard>().AnyAsync(g => g.Code == code))
            throw new ValidationException("Gift card code already exists");

        var giftCard = new GiftCard
        {
            Code = code,
            InitialBalance = dto.InitialBalance,
            CurrentBalance = dto.InitialBalance,
            Currency = dto.Currency ?? "TRY",
            Status = GiftCardStatus.Active,
            ExpiresAt = dto.ExpiresAt,
            RecipientEmail = dto.RecipientEmail,
            RecipientName = dto.RecipientName,
            PersonalMessage = dto.PersonalMessage,
            AdminNotes = dto.AdminNotes
        };

        _db.Set<GiftCard>().Add(giftCard);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetGiftCard), new { id = giftCard.Id }, new
        {
            Message = "Gift card created",
            giftCard.Id,
            giftCard.Code,
            giftCard.InitialBalance,
            giftCard.ExpiresAt
        });
    }

    [HttpPost("bulk")]
    public async Task<ActionResult> CreateBulkGiftCards([FromBody] CreateBulkGiftCardsDto dto)
    {
        var giftCards = new List<GiftCard>();

        for (int i = 0; i < dto.Count; i++)
        {
            var code = GenerateGiftCardCode();
            while (await _db.Set<GiftCard>().AnyAsync(g => g.Code == code) || giftCards.Any(g => g.Code == code))
                code = GenerateGiftCardCode();

            giftCards.Add(new GiftCard
            {
                Code = code,
                InitialBalance = dto.InitialBalance,
                CurrentBalance = dto.InitialBalance,
                Currency = dto.Currency ?? "TRY",
                Status = GiftCardStatus.Active,
                ExpiresAt = dto.ExpiresAt,
                AdminNotes = dto.AdminNotes
            });
        }

        _db.Set<GiftCard>().AddRange(giftCards);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = $"{dto.Count} gift cards created",
            Codes = giftCards.Select(g => g.Code).ToList()
        });
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateGiftCardStatusDto dto)
    {
        var giftCard = await _db.Set<GiftCard>().FindAsync(id);
        if (giftCard == null) throw new NotFoundException("Gift card not found");

        giftCard.Status = dto.Status;
        if (!string.IsNullOrEmpty(dto.Notes))
            giftCard.AdminNotes = dto.Notes;

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Gift card status updated", Status = giftCard.Status });
    }

    [HttpPatch("{id:int}/balance")]
    public async Task<ActionResult> AdjustBalance(int id, [FromBody] AdjustGiftCardBalanceDto dto)
    {
        var giftCard = await _db.Set<GiftCard>().FindAsync(id);
        if (giftCard == null) throw new NotFoundException("Gift card not found");

        var oldBalance = giftCard.CurrentBalance;
        giftCard.CurrentBalance += dto.Amount;

        if (giftCard.CurrentBalance < 0)
            throw new ValidationException("Balance cannot be negative");

        var usage = new GiftCardUsage
        {
            GiftCardId = id,
            UserId = 1, // Admin user - should get from context
            Amount = dto.Amount,
            BalanceAfter = giftCard.CurrentBalance,
            Type = GiftCardUsageType.Adjustment,
            Description = dto.Description ?? "Manual adjustment by admin"
        };

        _db.Set<GiftCardUsage>().Add(usage);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = "Gift card balance adjusted",
            OldBalance = oldBalance,
            NewBalance = giftCard.CurrentBalance,
            Adjustment = dto.Amount
        });
    }

    [HttpPatch("{id:int}/extend")]
    public async Task<ActionResult> ExtendExpiration(int id, [FromBody] ExtendGiftCardDto dto)
    {
        var giftCard = await _db.Set<GiftCard>().FindAsync(id);
        if (giftCard == null) throw new NotFoundException("Gift card not found");

        giftCard.ExpiresAt = dto.NewExpiresAt;
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Gift card expiration extended", ExpiresAt = giftCard.ExpiresAt });
    }

    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics()
    {
        var giftCards = await _db.Set<GiftCard>().ToListAsync();
        var usages = await _db.Set<GiftCardUsage>().ToListAsync();

        var stats = new
        {
            TotalGiftCards = giftCards.Count,
            TotalIssuedValue = giftCards.Sum(g => g.InitialBalance),
            TotalRemainingBalance = giftCards.Sum(g => g.CurrentBalance),
            TotalUsedValue = giftCards.Sum(g => g.InitialBalance - g.CurrentBalance),

            ByStatus = giftCards
                .GroupBy(g => g.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count(), TotalBalance = g.Sum(x => x.CurrentBalance) })
                .ToList(),

            ActiveWithBalance = giftCards.Count(g => g.Status == GiftCardStatus.Active && g.CurrentBalance > 0),
            Expired = giftCards.Count(g => g.ExpiresAt.HasValue && g.ExpiresAt < DateTime.UtcNow),
            FullyUsed = giftCards.Count(g => g.CurrentBalance == 0),

            RecentUsages = usages
                .Where(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count(), TotalAmount = g.Sum(x => Math.Abs(x.Amount)) })
                .OrderBy(x => x.Date)
                .ToList()
        };

        return Ok(stats);
    }

    private static string GenerateGiftCardCode()
    {
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        var code = "GFT-";
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 4; j++)
                code += chars[random.Next(chars.Length)];
            if (i < 2) code += "-";
        }
        return code;
    }
}

public class CreateGiftCardDto
{
    public string? Code { get; set; }
    [Required] public decimal InitialBalance { get; set; }
    public string? Currency { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientName { get; set; }
    public string? PersonalMessage { get; set; }
    public string? AdminNotes { get; set; }
}

public class CreateBulkGiftCardsDto
{
    [Required][Range(1, 100)] public int Count { get; set; }
    [Required] public decimal InitialBalance { get; set; }
    public string? Currency { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? AdminNotes { get; set; }
}

public class UpdateGiftCardStatusDto
{
    public GiftCardStatus Status { get; set; }
    public string? Notes { get; set; }
}

public class AdjustGiftCardBalanceDto
{
    [Required] public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class ExtendGiftCardDto
{
    [Required] public DateTime NewExpiresAt { get; set; }
}

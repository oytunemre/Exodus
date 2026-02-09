using Exodus.Data;
using Exodus.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Exodus.Controllers.Admin;

[Route("api/admin/audit")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminAuditController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminAuditController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> GetAuditLogs(
        [FromQuery] int? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] int? entityId = null,
        [FromQuery] string? ipAddress = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] bool sortDesc = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _db.AuditLogs.Include(a => a.User).AsQueryable();

        if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);
        if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action.Contains(action));
        if (!string.IsNullOrEmpty(entityType)) query = query.Where(a => a.EntityType == entityType);
        if (entityId.HasValue) query = query.Where(a => a.EntityId == entityId.Value);
        if (!string.IsNullOrEmpty(ipAddress)) query = query.Where(a => a.IpAddress == ipAddress);
        if (fromDate.HasValue) query = query.Where(a => a.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(a => a.CreatedAt <= toDate.Value);

        query = sortBy?.ToLower() switch
        {
            "action" => sortDesc ? query.OrderByDescending(a => a.Action) : query.OrderBy(a => a.Action),
            "entitytype" => sortDesc ? query.OrderByDescending(a => a.EntityType) : query.OrderBy(a => a.EntityType),
            "userid" => sortDesc ? query.OrderByDescending(a => a.UserId) : query.OrderBy(a => a.UserId),
            _ => sortDesc ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var logs = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                UserEmail = a.User != null ? a.User.Email : null,
                UserName = a.User != null ? a.User.Name : null,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.OldValues,
                a.NewValues,
                a.IpAddress,
                a.UserAgent,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Items = logs,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetAuditLog(int id)
    {
        var log = await _db.AuditLogs
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (log == null)
            return NotFound(new { Message = "Audit log not found" });

        return Ok(new
        {
            log.Id,
            log.UserId,
            UserEmail = log.User?.Email,
            UserName = log.User != null ? log.User.Name : null,
            log.Action,
            log.EntityType,
            log.EntityId,
            log.OldValues,
            log.NewValues,
            log.IpAddress,
            log.UserAgent,
            log.CreatedAt
        });
    }

    [HttpGet("entity/{entityType}/{entityId:int}")]
    public async Task<ActionResult> GetEntityHistory(string entityType, int entityId)
    {
        var logs = await _db.AuditLogs
            .Include(a => a.User)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                UserEmail = a.User != null ? a.User.Email : null,
                a.Action,
                a.OldValues,
                a.NewValues,
                a.IpAddress,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(logs);
    }

    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult> GetUserActivity(int userId, [FromQuery] int days = 30)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);

        var logs = await _db.AuditLogs
            .Where(a => a.UserId == userId && a.CreatedAt >= fromDate)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.IpAddress,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(logs);
    }

    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics([FromQuery] int days = 30)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);

        var logs = await _db.AuditLogs
            .Where(a => a.CreatedAt >= fromDate)
            .ToListAsync();

        var stats = new
        {
            TotalActions = logs.Count,
            UniqueUsers = logs.Where(l => l.UserId.HasValue).Select(l => l.UserId).Distinct().Count(),
            UniqueIPs = logs.Where(l => !string.IsNullOrEmpty(l.IpAddress)).Select(l => l.IpAddress).Distinct().Count(),

            ByAction = logs
                .GroupBy(l => l.Action)
                .Select(g => new { Action = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(20)
                .ToList(),

            ByEntityType = logs
                .Where(l => !string.IsNullOrEmpty(l.EntityType))
                .GroupBy(l => l.EntityType)
                .Select(g => new { EntityType = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList(),

            ByDay = logs
                .GroupBy(l => l.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList(),

            TopUsers = logs
                .Where(l => l.UserId.HasValue)
                .GroupBy(l => l.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList()
        };

        return Ok(stats);
    }

    [HttpDelete("cleanup")]
    public async Task<ActionResult> CleanupOldLogs([FromQuery] int daysToKeep = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

        var oldLogs = await _db.AuditLogs
            .Where(a => a.CreatedAt < cutoffDate)
            .ToListAsync();

        var count = oldLogs.Count;
        _db.AuditLogs.RemoveRange(oldLogs);
        await _db.SaveChangesAsync();

        return Ok(new { Message = $"Deleted {count} audit logs older than {daysToKeep} days" });
    }
}

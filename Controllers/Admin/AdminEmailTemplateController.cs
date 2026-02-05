using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FarmazonDemo.Controllers.Admin;

[Route("api/admin/email-templates")]
[ApiController]
[Authorize(Policy = "AdminOnly")]
public class AdminEmailTemplateController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminEmailTemplateController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> GetTemplates(
        [FromQuery] EmailTemplateType? type = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null)
    {
        var query = _db.Set<EmailTemplate>().AsQueryable();

        if (type.HasValue) query = query.Where(t => t.Type == type.Value);
        if (isActive.HasValue) query = query.Where(t => t.IsActive == isActive.Value);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(t => t.Name.Contains(search) || t.Code.Contains(search) || t.Subject.Contains(search));

        var templates = await query.OrderBy(t => t.Type).ThenBy(t => t.Name).ToListAsync();

        return Ok(templates.Select(t => new
        {
            t.Id,
            t.Name,
            t.Code,
            t.Subject,
            t.Type,
            TypeName = t.Type.ToString(),
            t.IsActive,
            t.AvailableVariables,
            t.CreatedAt,
            t.UpdatedAt
        }));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetTemplate(int id)
    {
        var template = await _db.Set<EmailTemplate>().FindAsync(id);
        if (template == null) throw new NotFoundException("Template not found");
        return Ok(template);
    }

    [HttpGet("code/{code}")]
    public async Task<ActionResult> GetTemplateByCode(string code)
    {
        var template = await _db.Set<EmailTemplate>().FirstOrDefaultAsync(t => t.Code == code);
        if (template == null) throw new NotFoundException("Template not found");
        return Ok(template);
    }

    [HttpPost]
    public async Task<ActionResult> CreateTemplate([FromBody] CreateEmailTemplateDto dto)
    {
        if (await _db.Set<EmailTemplate>().AnyAsync(t => t.Code == dto.Code))
            throw new ValidationException("Template code already exists");

        var template = new EmailTemplate
        {
            Name = dto.Name,
            Code = dto.Code,
            Subject = dto.Subject,
            HtmlBody = dto.HtmlBody,
            TextBody = dto.TextBody,
            Type = dto.Type,
            IsActive = dto.IsActive,
            AvailableVariables = dto.AvailableVariables
        };

        _db.Set<EmailTemplate>().Add(template);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateTemplate(int id, [FromBody] UpdateEmailTemplateDto dto)
    {
        var template = await _db.Set<EmailTemplate>().FindAsync(id);
        if (template == null) throw new NotFoundException("Template not found");

        if (!string.IsNullOrEmpty(dto.Name)) template.Name = dto.Name;
        if (dto.Code != null)
        {
            if (await _db.Set<EmailTemplate>().AnyAsync(t => t.Code == dto.Code && t.Id != id))
                throw new ValidationException("Template code already exists");
            template.Code = dto.Code;
        }
        if (!string.IsNullOrEmpty(dto.Subject)) template.Subject = dto.Subject;
        if (!string.IsNullOrEmpty(dto.HtmlBody)) template.HtmlBody = dto.HtmlBody;
        if (dto.TextBody != null) template.TextBody = dto.TextBody;
        if (dto.Type.HasValue) template.Type = dto.Type.Value;
        if (dto.IsActive.HasValue) template.IsActive = dto.IsActive.Value;
        if (dto.AvailableVariables != null) template.AvailableVariables = dto.AvailableVariables;

        await _db.SaveChangesAsync();
        return Ok(template);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteTemplate(int id)
    {
        var template = await _db.Set<EmailTemplate>().FindAsync(id);
        if (template == null) throw new NotFoundException("Template not found");

        _db.Remove(template);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Template deleted" });
    }

    [HttpPost("{id:int}/preview")]
    public async Task<ActionResult> PreviewTemplate(int id, [FromBody] Dictionary<string, string>? variables = null)
    {
        var template = await _db.Set<EmailTemplate>().FindAsync(id);
        if (template == null) throw new NotFoundException("Template not found");

        var html = template.HtmlBody;
        var text = template.TextBody ?? "";
        var subject = template.Subject;

        if (variables != null)
        {
            foreach (var (key, value) in variables)
            {
                var placeholder = "{{" + key + "}}";
                html = html.Replace(placeholder, value);
                text = text.Replace(placeholder, value);
                subject = subject.Replace(placeholder, value);
            }
        }

        return Ok(new
        {
            Subject = subject,
            HtmlBody = html,
            TextBody = text
        });
    }

    [HttpPost("{id:int}/duplicate")]
    public async Task<ActionResult> DuplicateTemplate(int id)
    {
        var original = await _db.Set<EmailTemplate>().FindAsync(id);
        if (original == null) throw new NotFoundException("Template not found");

        var copy = new EmailTemplate
        {
            Name = original.Name + " (Copy)",
            Code = original.Code + "_COPY_" + DateTime.UtcNow.Ticks,
            Subject = original.Subject,
            HtmlBody = original.HtmlBody,
            TextBody = original.TextBody,
            Type = original.Type,
            IsActive = false,
            AvailableVariables = original.AvailableVariables
        };

        _db.Set<EmailTemplate>().Add(copy);
        await _db.SaveChangesAsync();

        return Ok(copy);
    }

    [HttpGet("types")]
    public ActionResult GetTemplateTypes()
    {
        var types = Enum.GetValues<EmailTemplateType>()
            .Select(t => new { Value = (int)t, Name = t.ToString() })
            .ToList();
        return Ok(types);
    }
}

public class CreateEmailTemplateDto
{
    [Required] public required string Name { get; set; }
    [Required] public required string Code { get; set; }
    [Required] public required string Subject { get; set; }
    [Required] public required string HtmlBody { get; set; }
    public string? TextBody { get; set; }
    public EmailTemplateType Type { get; set; }
    public bool IsActive { get; set; } = true;
    public string? AvailableVariables { get; set; }
}

public class UpdateEmailTemplateDto
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Subject { get; set; }
    public string? HtmlBody { get; set; }
    public string? TextBody { get; set; }
    public EmailTemplateType? Type { get; set; }
    public bool? IsActive { get; set; }
    public string? AvailableVariables { get; set; }
}

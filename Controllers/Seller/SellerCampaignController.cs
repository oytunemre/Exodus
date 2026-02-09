using Exodus.Models.Dto.Campaign;
using Exodus.Services.Campaigns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Exodus.Controllers.Seller;

[ApiController]
[Route("api/seller/campaigns")]
[Authorize(Policy = "SellerOnly")]
public class SellerCampaignController : ControllerBase
{
    private readonly ICampaignService _campaignService;

    public SellerCampaignController(ICampaignService campaignService)
    {
        _campaignService = campaignService;
    }

    private int GetSellerId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Create a new campaign
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CampaignDto>> Create([FromBody] CreateCampaignDto dto, CancellationToken ct)
    {
        var sellerId = GetSellerId();
        var result = await _campaignService.CreateAsync(sellerId, dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get all campaigns for the seller
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CampaignDto>>> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var sellerId = GetSellerId();
        var campaigns = await _campaignService.GetBySellerAsync(sellerId, includeInactive, ct);
        return Ok(campaigns);
    }

    /// <summary>
    /// Get campaign by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CampaignDto>> GetById(int id, CancellationToken ct)
    {
        var campaign = await _campaignService.GetByIdAsync(id, ct);

        // Check ownership
        var sellerId = GetSellerId();
        if (campaign.SellerId != sellerId)
            return Forbid();

        return Ok(campaign);
    }

    /// <summary>
    /// Update campaign
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CampaignDto>> Update(int id, [FromBody] UpdateCampaignDto dto, CancellationToken ct)
    {
        var sellerId = GetSellerId();
        var result = await _campaignService.UpdateAsync(id, sellerId, dto, ct);
        return Ok(result);
    }

    /// <summary>
    /// Delete campaign
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        var sellerId = GetSellerId();
        await _campaignService.DeleteAsync(id, sellerId, ct);
        return NoContent();
    }

    /// <summary>
    /// Toggle campaign active status
    /// </summary>
    [HttpPatch("{id:int}/toggle-active")]
    public async Task<ActionResult<CampaignDto>> ToggleActive(int id, CancellationToken ct)
    {
        var sellerId = GetSellerId();
        var result = await _campaignService.ToggleActiveAsync(id, sellerId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get campaign statistics
    /// </summary>
    [HttpGet("{id:int}/statistics")]
    public async Task<ActionResult<CampaignStatisticsDto>> GetStatistics(int id, CancellationToken ct)
    {
        var sellerId = GetSellerId();
        var stats = await _campaignService.GetStatisticsAsync(id, sellerId, ct);
        return Ok(stats);
    }

    /// <summary>
    /// Get campaign usage history
    /// </summary>
    [HttpGet("{id:int}/usage")]
    public async Task<ActionResult<List<CampaignUsageDto>>> GetUsageHistory(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var sellerId = GetSellerId();
        var usages = await _campaignService.GetUsageHistoryAsync(id, sellerId, page, pageSize, ct);
        return Ok(usages);
    }
}

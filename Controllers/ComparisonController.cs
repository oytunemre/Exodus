using FarmazonDemo.Services.Comparison;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FarmazonDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComparisonController : ControllerBase
{
    private readonly IProductComparisonService _comparisonService;

    public ComparisonController(IProductComparisonService comparisonService)
    {
        _comparisonService = comparisonService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Yeni karsilastirma listesi olustur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateComparisonDto? dto, CancellationToken ct)
    {
        var comparison = await _comparisonService.CreateComparisonAsync(GetUserId(), dto?.Name, ct);
        return CreatedAtAction(nameof(GetComparison), new { id = comparison.Id }, comparison);
    }

    /// <summary>
    /// Karsilastirma listesine urun ekle
    /// </summary>
    [HttpPost("{id}/products/{productId}")]
    public async Task<IActionResult> AddProduct(int id, int productId, CancellationToken ct)
    {
        var comparison = await _comparisonService.AddProductAsync(GetUserId(), id, productId, ct);
        return Ok(comparison);
    }

    /// <summary>
    /// Karsilastirma listesinden urun cikar
    /// </summary>
    [HttpDelete("{id}/products/{productId}")]
    public async Task<IActionResult> RemoveProduct(int id, int productId, CancellationToken ct)
    {
        var comparison = await _comparisonService.RemoveProductAsync(GetUserId(), id, productId, ct);
        return Ok(comparison);
    }

    /// <summary>
    /// Karsilastirma listesini getir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetComparison(int id, CancellationToken ct)
    {
        var comparison = await _comparisonService.GetComparisonAsync(GetUserId(), id, ct);
        return Ok(comparison);
    }

    /// <summary>
    /// Kullanicinin tum karsilastirma listelerini getir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var comparisons = await _comparisonService.GetUserComparisonsAsync(GetUserId(), ct);
        return Ok(comparisons);
    }

    /// <summary>
    /// Karsilastirma listesini sil
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _comparisonService.DeleteComparisonAsync(GetUserId(), id, ct);
        return NoContent();
    }

    /// <summary>
    /// Detayli karsilastirma (ozellikler, fiyatlar, puanlar ile)
    /// </summary>
    [HttpGet("{id}/detailed")]
    public async Task<IActionResult> GetDetailed(int id, CancellationToken ct)
    {
        var detail = await _comparisonService.GetDetailedComparisonAsync(GetUserId(), id, ct);
        return Ok(detail);
    }
}

public class CreateComparisonDto
{
    public string? Name { get; set; }
}

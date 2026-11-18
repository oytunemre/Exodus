using System.Security.Claims;
using Exodus.Models.Dto;
using Exodus.Models.Dto.ListingDto;
using Exodus.Services.Common;
using Exodus.Services.Listings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exodus.Controllers;

[Route("api/listings")]
[ApiController]
public class ListingController : ControllerBase
{
    private readonly IListingService _service;

    public ListingController(IListingService service)
    {
        _service = service;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedException("Invalid or missing user identity");

        return userId;
    }

    private bool IsAdmin() => User.IsInRole("Admin");

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    /// <summary>
    /// Public paged search over active listings (filters: search, categoryId, productId, price range,
    /// condition, sellerId, inStock, stockStatus; sort: price, createdAt, name, stock, seller).
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] ListingSearchDto query, CancellationToken ct)
        => Ok(await _service.SearchAsync(query, ct));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
        => Ok(await _service.GetByIdAsync(id));

    [HttpPost]
    [Authorize(Roles = "Admin,Seller")]
    public async Task<IActionResult> Create([FromBody] AddListingDto dto) // <-- AddListingDto
        => Ok(await _service.CreateAsync(dto, GetCurrentUserId(), IsAdmin()));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Seller")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateListingDto dto)
        => Ok(await _service.UpdateAsync(id, dto, GetCurrentUserId(), IsAdmin()));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Seller")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.SoftDeleteAsync(id, GetCurrentUserId(), IsAdmin());
        return NoContent();
    }
}

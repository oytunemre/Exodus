using Exodus.Services.Addresses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Exodus.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AddressController : ControllerBase
{
    private readonly IAddressService _addressService;

    public AddressController(IAddressService addressService)
    {
        _addressService = addressService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Kullanicinin tum adreslerini listeler
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var addresses = await _addressService.GetAllAsync(GetUserId(), ct);
        return Ok(addresses);
    }

    /// <summary>
    /// Belirli bir adresi getirir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var address = await _addressService.GetByIdAsync(GetUserId(), id, ct);
        return Ok(address);
    }

    /// <summary>
    /// Yeni adres ekler
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAddressDto dto, CancellationToken ct)
    {
        var address = await _addressService.CreateAsync(GetUserId(), dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = address.Id }, address);
    }

    /// <summary>
    /// Adresi gunceller
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAddressDto dto, CancellationToken ct)
    {
        var address = await _addressService.UpdateAsync(GetUserId(), id, dto, ct);
        return Ok(address);
    }

    /// <summary>
    /// Adresi siler
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _addressService.DeleteAsync(GetUserId(), id, ct);
        return NoContent();
    }

    /// <summary>
    /// Adresi varsayilan olarak ayarlar
    /// </summary>
    [HttpPatch("{id}/set-default")]
    public async Task<IActionResult> SetDefault(int id, CancellationToken ct)
    {
        var address = await _addressService.SetDefaultAsync(GetUserId(), id, ct);
        return Ok(address);
    }
}

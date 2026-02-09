using Exodus.Data;
using Exodus.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Exodus.Services.Addresses;

public class AddressService : IAddressService
{
    private readonly ApplicationDbContext _db;

    public AddressService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AddressResponseDto> CreateAsync(int userId, CreateAddressDto dto, CancellationToken ct = default)
    {
        if (!Enum.TryParse<AddressType>(dto.Type, true, out var addressType))
            addressType = AddressType.Shipping;

        // Eger default olarak isaretlenmisse diger adreslerin default'unu kaldir
        if (dto.IsDefault)
        {
            var existingDefaults = await _db.Addresses
                .Where(a => a.UserId == userId && a.IsDefault)
                .ToListAsync(ct);
            foreach (var addr in existingDefaults)
                addr.IsDefault = false;
        }

        var address = new Address
        {
            UserId = userId,
            Title = dto.Title,
            FullName = dto.FullName,
            Phone = dto.Phone,
            City = dto.City,
            District = dto.District,
            Neighborhood = dto.Neighborhood,
            AddressLine = dto.AddressLine,
            PostalCode = dto.PostalCode,
            Type = addressType,
            IsDefault = dto.IsDefault
        };

        _db.Addresses.Add(address);
        await _db.SaveChangesAsync(ct);

        return MapToDto(address);
    }

    public async Task<AddressResponseDto> UpdateAsync(int userId, int addressId, UpdateAddressDto dto, CancellationToken ct = default)
    {
        var address = await _db.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Adres bulunamadi");

        if (dto.Title != null) address.Title = dto.Title;
        if (dto.FullName != null) address.FullName = dto.FullName;
        if (dto.Phone != null) address.Phone = dto.Phone;
        if (dto.City != null) address.City = dto.City;
        if (dto.District != null) address.District = dto.District;
        if (dto.Neighborhood != null) address.Neighborhood = dto.Neighborhood;
        if (dto.AddressLine != null) address.AddressLine = dto.AddressLine;
        if (dto.PostalCode != null) address.PostalCode = dto.PostalCode;
        if (dto.Type != null && Enum.TryParse<AddressType>(dto.Type, true, out var addressType))
            address.Type = addressType;

        if (dto.IsDefault == true)
        {
            var existingDefaults = await _db.Addresses
                .Where(a => a.UserId == userId && a.IsDefault && a.Id != addressId)
                .ToListAsync(ct);
            foreach (var addr in existingDefaults)
                addr.IsDefault = false;
            address.IsDefault = true;
        }
        else if (dto.IsDefault == false)
        {
            address.IsDefault = false;
        }

        await _db.SaveChangesAsync(ct);
        return MapToDto(address);
    }

    public async Task DeleteAsync(int userId, int addressId, CancellationToken ct = default)
    {
        var address = await _db.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Adres bulunamadi");

        _db.Addresses.Remove(address);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AddressResponseDto> GetByIdAsync(int userId, int addressId, CancellationToken ct = default)
    {
        var address = await _db.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Adres bulunamadi");

        return MapToDto(address);
    }

    public async Task<List<AddressResponseDto>> GetAllAsync(int userId, CancellationToken ct = default)
    {
        var addresses = await _db.Addresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.UpdatedAt)
            .ToListAsync(ct);

        return addresses.Select(MapToDto).ToList();
    }

    public async Task<AddressResponseDto> SetDefaultAsync(int userId, int addressId, CancellationToken ct = default)
    {
        var address = await _db.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Adres bulunamadi");

        var existingDefaults = await _db.Addresses
            .Where(a => a.UserId == userId && a.IsDefault && a.Id != addressId)
            .ToListAsync(ct);
        foreach (var addr in existingDefaults)
            addr.IsDefault = false;

        address.IsDefault = true;
        await _db.SaveChangesAsync(ct);

        return MapToDto(address);
    }

    private static AddressResponseDto MapToDto(Address address)
    {
        return new AddressResponseDto
        {
            Id = address.Id,
            Title = address.Title,
            FullName = address.FullName,
            Phone = address.Phone,
            City = address.City,
            District = address.District,
            Neighborhood = address.Neighborhood,
            AddressLine = address.AddressLine,
            PostalCode = address.PostalCode,
            Type = address.Type.ToString(),
            IsDefault = address.IsDefault,
            CreatedAt = address.CreatedAt,
            UpdatedAt = address.UpdatedAt
        };
    }
}

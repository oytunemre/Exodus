namespace Exodus.Services.Addresses;

public interface IAddressService
{
    Task<AddressResponseDto> CreateAsync(int userId, CreateAddressDto dto, CancellationToken ct = default);
    Task<AddressResponseDto> UpdateAsync(int userId, int addressId, UpdateAddressDto dto, CancellationToken ct = default);
    Task DeleteAsync(int userId, int addressId, CancellationToken ct = default);
    Task<AddressResponseDto> GetByIdAsync(int userId, int addressId, CancellationToken ct = default);
    Task<List<AddressResponseDto>> GetAllAsync(int userId, CancellationToken ct = default);
    Task<AddressResponseDto> SetDefaultAsync(int userId, int addressId, CancellationToken ct = default);
}

public class CreateAddressDto
{
    public string Title { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? Neighborhood { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string Type { get; set; } = "Shipping"; // Shipping, Billing, Both
    public bool IsDefault { get; set; } = false;
}

public class UpdateAddressDto
{
    public string? Title { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighborhood { get; set; }
    public string? AddressLine { get; set; }
    public string? PostalCode { get; set; }
    public string? Type { get; set; }
    public bool? IsDefault { get; set; }
}

public class AddressResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? Neighborhood { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

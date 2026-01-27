using System.ComponentModel.DataAnnotations;
using FarmazonDemo.Models.Entities;

namespace FarmazonDemo.Models.Dto
{
    public class CreateAddressDto
    {
        [Required]
        [StringLength(100)]
        public required string Title { get; set; }

        [Required]
        [StringLength(100)]
        public required string FullName { get; set; }

        [Required]
        [Phone]
        [StringLength(20)]
        public required string Phone { get; set; }

        [Required]
        [StringLength(100)]
        public required string City { get; set; }

        [Required]
        [StringLength(100)]
        public required string District { get; set; }

        [StringLength(100)]
        public string? Neighborhood { get; set; }

        [Required]
        [StringLength(500)]
        public required string AddressLine { get; set; }

        [StringLength(10)]
        public string? PostalCode { get; set; }

        public bool IsDefault { get; set; } = false;

        public AddressType Type { get; set; } = AddressType.Shipping;
    }

    public class UpdateAddressDto
    {
        [StringLength(100)]
        public string? Title { get; set; }

        [StringLength(100)]
        public string? FullName { get; set; }

        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? District { get; set; }

        [StringLength(100)]
        public string? Neighborhood { get; set; }

        [StringLength(500)]
        public string? AddressLine { get; set; }

        [StringLength(10)]
        public string? PostalCode { get; set; }

        public bool? IsDefault { get; set; }

        public AddressType? Type { get; set; }
    }

    public class AddressResponseDto
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string FullName { get; set; }
        public required string Phone { get; set; }
        public required string City { get; set; }
        public required string District { get; set; }
        public string? Neighborhood { get; set; }
        public required string AddressLine { get; set; }
        public string? PostalCode { get; set; }
        public bool IsDefault { get; set; }
        public AddressType Type { get; set; }
        public string FullAddress => $"{AddressLine}, {Neighborhood ?? ""} {District}/{City} {PostalCode ?? ""}".Trim();
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmazonDemo.Models.Entities
{
    public class Address : BaseEntity
    {
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public required string Title { get; set; } // Ev, İş, vb.

        [Required]
        [StringLength(100)]
        public required string FullName { get; set; }

        [Required]
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

    public enum AddressType
    {
        Shipping,
        Billing,
        Both
    }
}

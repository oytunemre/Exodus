using System.ComponentModel.DataAnnotations;
using Exodus.Models.Enums;

namespace Exodus.Models.Dto
{
    public class DevForceVerifyDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        /// <summary>Optional: update the user's role (Admin, Seller, Customer)</summary>
        public UserRole? Role { get; set; }
    }
}

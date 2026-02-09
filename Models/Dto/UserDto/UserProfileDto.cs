using System.ComponentModel.DataAnnotations;
using Exodus.Models.Enums;

namespace Exodus.Models.Dto
{
    public class UserProfileResponseDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Username { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public UserRole Role { get; set; }
        public bool EmailVerified { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class UpdateProfileDto
    {
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(50)]
        public string? Username { get; set; }

        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Url]
        [StringLength(500)]
        public string? AvatarUrl { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required]
        public required string CurrentPassword { get; set; }

        [Required]
        [MinLength(8)]
        public required string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword")]
        public required string ConfirmPassword { get; set; }
    }

    public class UserStatsDto
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public int WishlistCount { get; set; }
        public int AddressCount { get; set; }
        public int UnreadNotifications { get; set; }
    }
}

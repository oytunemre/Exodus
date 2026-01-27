using System.ComponentModel.DataAnnotations;
using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Models.Entities
{
    public class Users : BaseEntity
    {
        public required string Name { get; set; } = string.Empty;
        public required string Email { get; set; } = string.Empty;
        public required string Password { get; set; } = string.Empty;
        public required string Username { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Customer;

        // Profile
        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? AvatarUrl { get; set; }

        public DateTime? LastLoginAt { get; set; }

        // Email Verification
        public bool EmailVerified { get; set; } = false;
        public string? EmailVerificationToken { get; set; }
        public DateTime? EmailVerificationTokenExpiresAt { get; set; }

        // Password Reset
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiresAt { get; set; }

        // Account Lockout
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEndTime { get; set; }

        // Two-Factor Authentication
        public bool TwoFactorEnabled { get; set; } = false;
        public string? TwoFactorSecretKey { get; set; }
        public string? TwoFactorBackupCodes { get; set; }
        public DateTime? TwoFactorEnabledAt { get; set; }
    }
}

using Exodus.Models.Enums;

namespace Exodus.Models.Dto
{
    public class RegisterDto
    {
        public required string Name { get; set; } = string.Empty;
        public required string Email { get; set; } = string.Empty;
        public required string Username { get; set; } = string.Empty;
        public required string Password { get; set; } = string.Empty;
        /// <summary>
        /// Ignored by the API: self-registered accounts are always created as <see cref="UserRole.Customer"/>.
        /// Roles are granted by an administrator.
        /// </summary>
        public UserRole Role { get; set; } = UserRole.Customer;
    }
}

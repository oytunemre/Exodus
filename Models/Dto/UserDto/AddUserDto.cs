namespace Exodus.Models.Dto.UserDto
{
    public class AdduserDto
    {
        public required string Name { get; set; } = string.Empty;
        public required string Email { get; set; } = string.Empty;
        public required string Password { get; set; } = string.Empty;
        public required string Username { get; set; } = string.Empty;

    }
}

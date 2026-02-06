using System.ComponentModel.DataAnnotations;

namespace Exodus.Models.Dto
{
    public class EmailRequestDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }
    }
}

using FluentValidation;
using FarmazonDemo.Models.Dto.UserDto;

namespace FarmazonDemo.Validation.Users;

public class AddUserDtoValidator : AbstractValidator<AdduserDto>
{
    public AddUserDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(200);
    }
}

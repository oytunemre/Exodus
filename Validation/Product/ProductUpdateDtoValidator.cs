using FarmazonDemo.Models.Dto.ProductDto;
using FluentValidation;

public class ProductUpdateDtoValidator : AbstractValidator<ProductUpdateDto>
{
    public ProductUpdateDtoValidator()
    {
        RuleFor(x => x.ProductName).NotEmpty().MinimumLength(2).MaximumLength(200);
        RuleFor(x => x.ProductDescription).NotEmpty().MinimumLength(5).MaximumLength(2000);
        RuleFor(x => x.ProductBarcode).NotEmpty().MinimumLength(3).MaximumLength(100);
    }
}
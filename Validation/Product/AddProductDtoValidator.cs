using FarmazonDemo.Models.Dto.ProductDto;
using FluentValidation;

namespace FarmazonDemo.Validation.Products;

public class AddProductDtoValidator : AbstractValidator<AddProductDto>
{
    public AddProductDtoValidator()
    {
        RuleFor(x => x.ProductName).NotEmpty().MinimumLength(2).MaximumLength(200);
        RuleFor(x => x.ProductDescription).NotEmpty().MinimumLength(5).MaximumLength(2000);

        RuleFor(x => x.Barcodes)
            .NotEmpty()
            .Must(b => b.Distinct(StringComparer.OrdinalIgnoreCase).Count() == b.Count)
            .WithMessage("Barcodes benzersiz olmalı.");

        RuleForEach(x => x.Barcodes)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(100);
    }
}

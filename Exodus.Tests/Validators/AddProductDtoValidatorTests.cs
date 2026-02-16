using Exodus.Models.Dto.ProductDto;
using Exodus.Validation.Products;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Exodus.Tests.Validators;

public class AddProductDtoValidatorTests
{
    private readonly AddProductDtoValidator _validator = new();

    private static AddProductDto ValidDto() => new()
    {
        ProductName = "Test Product",
        ProductDescription = "This is a test product description",
        Barcodes = new List<string> { "BARCODE001" }
    };

    #region ProductName

    [Fact]
    public void ProductName_WhenEmpty_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.ProductName = "";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ProductName);
    }

    [Fact]
    public void ProductName_WhenTooShort_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.ProductName = "A";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ProductName);
    }

    [Fact]
    public void ProductName_WhenTooLong_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.ProductName = new string('A', 201);
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ProductName);
    }

    [Fact]
    public void ProductName_WhenValid_ShouldNotHaveError()
    {
        var dto = ValidDto();
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.ProductName);
    }

    #endregion

    #region ProductDescription

    [Fact]
    public void ProductDescription_WhenEmpty_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.ProductDescription = "";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ProductDescription);
    }

    [Fact]
    public void ProductDescription_WhenTooShort_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.ProductDescription = "Ab";
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ProductDescription);
    }

    [Fact]
    public void ProductDescription_WhenTooLong_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.ProductDescription = new string('A', 2001);
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ProductDescription);
    }

    #endregion

    #region Barcodes

    [Fact]
    public void Barcodes_WhenEmpty_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Barcodes = new List<string>();
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Barcodes);
    }

    [Fact]
    public void Barcodes_WhenContainsDuplicates_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Barcodes = new List<string> { "BARCODE001", "BARCODE001" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Barcodes);
    }

    [Fact]
    public void Barcodes_WhenItemTooShort_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Barcodes = new List<string> { "AB" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Barcodes_WhenItemTooLong_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Barcodes = new List<string> { new string('A', 101) };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Barcodes_WhenItemEmpty_ShouldHaveError()
    {
        var dto = ValidDto();
        dto.Barcodes = new List<string> { "" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Barcodes_WhenValidMultiple_ShouldNotHaveError()
    {
        var dto = ValidDto();
        dto.Barcodes = new List<string> { "BARCODE001", "BARCODE002", "BARCODE003" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Barcodes);
    }

    #endregion

    [Fact]
    public void ValidDto_ShouldPassAllValidation()
    {
        var dto = ValidDto();
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

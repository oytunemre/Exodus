namespace FarmazonDemo.Models.Dto.ProductDto
{
    public class ProductUpdateDto
    {
        public required string ProductName { get; set; }
        public required string ProductDescription { get; set; }

        public List<string> Barcodes { get; set; } = new();
    }
}

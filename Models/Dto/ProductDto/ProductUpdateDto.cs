namespace Exodus.Models.Dto.ProductDto
{
    public class ProductUpdateDto
    {
        public required string ProductName { get; set; }
        public required string ProductDescription { get; set; }
        public string? Brand { get; set; }
        public string? Manufacturer { get; set; }
        public int? CategoryId { get; set; }
        public List<string> Barcodes { get; set; } = new();
    }
}

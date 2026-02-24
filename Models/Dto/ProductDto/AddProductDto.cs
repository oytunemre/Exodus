namespace Exodus.Models.Dto.ProductDto
{
    public class AddProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public string? Manufacturer { get; set; }
        public int? CategoryId { get; set; }
        public List<string> Barcodes { get; set; } = new();
    }
}

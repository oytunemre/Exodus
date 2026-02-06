namespace Exodus.Models.Dto.ProductDto
{
    public class AddProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;

        public List<string> Barcodes { get; set; } = new();
    }
}

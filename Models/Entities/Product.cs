namespace FarmazonDemo.Models.Entities
{
    public class Product : BaseEntity
    {
        public required string ProductName { get; set; } = string.Empty;
        public required string ProductDescription { get; set; } = string.Empty;

        public ICollection<ProductBarcode> Barcodes { get; set; } = new List<ProductBarcode>();
    }
}

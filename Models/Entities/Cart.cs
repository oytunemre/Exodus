namespace FarmazonDemo.Models.Entities
{
    public class Cart : BaseEntity
    {
        public int UserId { get; set; }
        public Users User { get; set; } = null!;

        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}

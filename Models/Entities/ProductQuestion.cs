using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmazonDemo.Models.Entities;

public class ProductQuestion : BaseEntity
{
    [Required]
    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    [Required]
    public int AskedByUserId { get; set; }

    [ForeignKey(nameof(AskedByUserId))]
    public Users AskedByUser { get; set; } = null!;

    [Required]
    [StringLength(1000)]
    public string QuestionText { get; set; } = string.Empty;

    public QuestionStatus Status { get; set; } = QuestionStatus.Pending;

    public int UpvoteCount { get; set; } = 0;

    public ICollection<ProductAnswer> Answers { get; set; } = new List<ProductAnswer>();
}

public class ProductAnswer : BaseEntity
{
    [Required]
    public int QuestionId { get; set; }

    [ForeignKey(nameof(QuestionId))]
    public ProductQuestion Question { get; set; } = null!;

    [Required]
    public int AnsweredByUserId { get; set; }

    [ForeignKey(nameof(AnsweredByUserId))]
    public Users AnsweredByUser { get; set; } = null!;

    [Required]
    [StringLength(2000)]
    public string AnswerText { get; set; } = string.Empty;

    public bool IsSellerAnswer { get; set; } = false;

    public bool IsAccepted { get; set; } = false;

    public int UpvoteCount { get; set; } = 0;
}

public enum QuestionStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Answered = 3
}

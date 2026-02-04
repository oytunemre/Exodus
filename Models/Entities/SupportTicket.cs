using System.ComponentModel.DataAnnotations;

namespace FarmazonDemo.Models.Entities;

public class SupportTicket : BaseEntity
{
    [Required]
    [StringLength(20)]
    public required string TicketNumber { get; set; }

    public int UserId { get; set; }
    public Users User { get; set; } = null!;

    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    public int? SellerOrderId { get; set; }
    public SellerOrder? SellerOrder { get; set; }

    [Required]
    [StringLength(200)]
    public required string Subject { get; set; }

    public TicketCategory Category { get; set; } = TicketCategory.General;

    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public int? AssignedToId { get; set; }
    public Users? AssignedTo { get; set; }

    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    // Customer satisfaction rating (1-5)
    public int? SatisfactionRating { get; set; }

    [StringLength(500)]
    public string? SatisfactionComment { get; set; }

    public ICollection<SupportTicketMessage> Messages { get; set; } = new List<SupportTicketMessage>();
}

public class SupportTicketMessage : BaseEntity
{
    public int TicketId { get; set; }
    public SupportTicket Ticket { get; set; } = null!;

    public int SenderId { get; set; }
    public Users Sender { get; set; } = null!;

    [Required]
    public required string Message { get; set; }

    // Comma-separated attachment URLs
    [StringLength(2000)]
    public string? Attachments { get; set; }

    // Internal notes visible only to admins
    public bool IsInternal { get; set; } = false;

    // Auto-generated system messages
    public bool IsSystemMessage { get; set; } = false;
}

public enum TicketCategory
{
    General,
    Order,
    Shipping,
    Product,
    Payment,
    Refund,
    Seller,
    Technical,
    Other
}

public enum TicketPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public enum TicketStatus
{
    Open,
    AwaitingCustomerReply,
    AwaitingSupport,
    InProgress,
    OnHold,
    Resolved,
    Closed
}

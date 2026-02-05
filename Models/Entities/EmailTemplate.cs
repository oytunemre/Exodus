using System.ComponentModel.DataAnnotations;

namespace FarmazonDemo.Models.Entities;

/// <summary>
/// E-posta şablonları
/// </summary>
public class EmailTemplate : BaseEntity
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; } // OrderConfirmation, ShippingNotification, etc.

    [Required]
    [StringLength(50)]
    public required string Code { get; set; } // ORDER_CONFIRMATION, SHIPPING_NOTIFICATION

    [Required]
    [StringLength(200)]
    public required string Subject { get; set; }

    [Required]
    public required string HtmlBody { get; set; }

    public string? TextBody { get; set; }

    public EmailTemplateType Type { get; set; }

    public bool IsActive { get; set; } = true;

    // Available variables (JSON array)
    [StringLength(2000)]
    public string? AvailableVariables { get; set; } // ["{{customer_name}}", "{{order_number}}"]
}

public enum EmailTemplateType
{
    // Account
    Welcome = 0,
    EmailVerification = 1,
    PasswordReset = 2,
    AccountDeactivated = 3,

    // Order
    OrderConfirmation = 10,
    OrderCancelled = 11,
    OrderShipped = 12,
    OrderDelivered = 13,

    // Payment
    PaymentSuccess = 20,
    PaymentFailed = 21,
    RefundProcessed = 22,

    // Seller
    SellerApplicationReceived = 30,
    SellerApproved = 31,
    SellerRejected = 32,
    SellerPayoutProcessed = 33,

    // Support
    TicketCreated = 40,
    TicketReplied = 41,
    TicketResolved = 42,

    // Marketing
    Newsletter = 50,
    PromotionalCampaign = 51,
    AbandonedCart = 52,

    // Other
    Custom = 99
}

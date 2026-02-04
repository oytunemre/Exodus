using FarmazonDemo.Models.Enums;

namespace FarmazonDemo.Services.PaymentGateway;

public interface IPaymentGateway
{
    string ProviderName { get; }

    Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentRequest request, CancellationToken ct = default);

    Task<PaymentResult> CompletePaymentAsync(string paymentToken, CancellationToken ct = default);

    Task<Payment3DSResult> Initiate3DSPaymentAsync(PaymentRequest request, CancellationToken ct = default);

    Task<PaymentResult> Complete3DSPaymentAsync(string paymentToken, CancellationToken ct = default);

    Task<RefundResult> RefundAsync(string paymentTransactionId, decimal? amount = null, CancellationToken ct = default);

    Task<PaymentStatusResult> GetPaymentStatusAsync(string paymentTransactionId, CancellationToken ct = default);

    Task<BinCheckResult> CheckBinAsync(string binNumber, CancellationToken ct = default);

    Task<InstallmentResult> GetInstallmentOptionsAsync(string binNumber, decimal price, CancellationToken ct = default);

    bool ValidateWebhookSignature(string payload, string signature);
}

public class PaymentRequest
{
    public string ConversationId { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal PaidPrice { get; set; }
    public string Currency { get; set; } = "TRY";
    public int? InstallmentCount { get; set; }
    public string? BasketId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    // Card Details
    public CardInfo? Card { get; set; }

    // Buyer Info
    public BuyerInfo Buyer { get; set; } = new();

    // Address Info
    public AddressInfo ShippingAddress { get; set; } = new();
    public AddressInfo BillingAddress { get; set; } = new();

    // Basket Items
    public List<BasketItemInfo> BasketItems { get; set; } = new();

    // Callback URLs
    public string? CallbackUrl { get; set; }
}

public class CardInfo
{
    public string CardHolderName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public string ExpireMonth { get; set; } = string.Empty;
    public string ExpireYear { get; set; } = string.Empty;
    public string Cvc { get; set; } = string.Empty;
    public bool RegisterCard { get; set; }
    public string? CardAlias { get; set; }
    public string? CardToken { get; set; }
    public string? CardUserKey { get; set; }
}

public class BuyerInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string IdentityNumber { get; set; } = "11111111111";
    public string Ip { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = "Turkey";
    public string RegistrationAddress { get; set; } = string.Empty;
}

public class AddressInfo
{
    public string ContactName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = "Turkey";
    public string Address { get; set; } = string.Empty;
    public string? ZipCode { get; set; }
}

public class BasketItemInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category1 { get; set; } = string.Empty;
    public string? Category2 { get; set; }
    public string ItemType { get; set; } = "PHYSICAL";
    public decimal Price { get; set; }
    public string? SubMerchantKey { get; set; }
    public decimal? SubMerchantPrice { get; set; }
}

public class PaymentInitiationResult
{
    public bool Success { get; set; }
    public string? PaymentId { get; set; }
    public string? PaymentTransactionId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CardLast4 { get; set; }
    public string? CardBrand { get; set; }
    public decimal? PaidPrice { get; set; }
    public string? Currency { get; set; }
    public int? Installment { get; set; }
}

public class Payment3DSResult
{
    public bool Success { get; set; }
    public string? PaymentId { get; set; }
    public string? ThreeDSHtmlContent { get; set; }
    public string? RedirectUrl { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string? PaymentId { get; set; }
    public string? PaymentTransactionId { get; set; }
    public decimal? PaidPrice { get; set; }
    public string? Currency { get; set; }
    public int? Installment { get; set; }
    public string? CardLast4 { get; set; }
    public string? CardBrand { get; set; }
    public string? CardAssociation { get; set; }
    public string? CardFamily { get; set; }
    public string? CardType { get; set; }
    public string? FraudStatus { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? AuthCode { get; set; }
    public string? HostReference { get; set; }
}

public class RefundResult
{
    public bool Success { get; set; }
    public string? PaymentId { get; set; }
    public string? PaymentTransactionId { get; set; }
    public decimal? RefundedAmount { get; set; }
    public string? Currency { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PaymentStatusResult
{
    public bool Success { get; set; }
    public string? PaymentId { get; set; }
    public string? Status { get; set; }
    public decimal? PaidPrice { get; set; }
    public string? Currency { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class BinCheckResult
{
    public bool Success { get; set; }
    public string? BinNumber { get; set; }
    public string? CardType { get; set; }
    public string? CardAssociation { get; set; }
    public string? CardFamily { get; set; }
    public string? BankName { get; set; }
    public long? BankCode { get; set; }
    public bool? Commercial { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class InstallmentResult
{
    public bool Success { get; set; }
    public List<InstallmentOption> Options { get; set; } = new();
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class InstallmentOption
{
    public string? BankName { get; set; }
    public long? BankCode { get; set; }
    public string? CardType { get; set; }
    public string? CardAssociation { get; set; }
    public string? CardFamily { get; set; }
    public bool? Force3DS { get; set; }
    public List<InstallmentDetail> Details { get; set; } = new();
}

public class InstallmentDetail
{
    public int InstallmentNumber { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal InstallmentPrice { get; set; }
    public decimal? InstallmentRate { get; set; }
}

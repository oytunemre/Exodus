using System.ComponentModel.DataAnnotations;

namespace Exodus.Models.Dto.Payment;

public class ProcessGatewayPaymentDto
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    public GatewayCardDto Card { get; set; } = new();

    public int? InstallmentCount { get; set; }

    public bool Use3DSecure { get; set; } = true;

    public string? CallbackUrl { get; set; }

    public string? IpAddress { get; set; }
}

public class GatewayCardDto
{
    [Required]
    [StringLength(100)]
    public string CardHolderName { get; set; } = string.Empty;

    [Required]
    [StringLength(19)]
    public string CardNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(2)]
    public string ExpireMonth { get; set; } = string.Empty;

    [Required]
    [StringLength(4)]
    public string ExpireYear { get; set; } = string.Empty;

    [Required]
    [StringLength(4)]
    public string Cvc { get; set; } = string.Empty;

    public bool RegisterCard { get; set; }

    public string? CardToken { get; set; }
    public string? CardUserKey { get; set; }
}

public class IyzicoPaymentResponseDto
{
    public bool Success { get; set; }
    public int? PaymentIntentId { get; set; }
    public string? GatewayPaymentId { get; set; }
    public string? GatewayTransactionId { get; set; }
    public decimal? PaidPrice { get; set; }
    public string? Currency { get; set; }
    public int? Installment { get; set; }
    public string? CardLast4 { get; set; }
    public string? CardBrand { get; set; }
    public string? CardFamily { get; set; }
    public string? CardType { get; set; }
    public string? FraudStatus { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class Iyzico3DSResponseDto
{
    public bool Success { get; set; }
    public int? PaymentIntentId { get; set; }
    public string? ThreeDSHtmlContent { get; set; }
    public string? RedirectUrl { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class BinCheckResponseDto
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

public class InstallmentResponseDto
{
    public bool Success { get; set; }
    public List<InstallmentOptionDto> Options { get; set; } = new();
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class InstallmentOptionDto
{
    public string? BankName { get; set; }
    public long? BankCode { get; set; }
    public string? CardType { get; set; }
    public string? CardAssociation { get; set; }
    public string? CardFamily { get; set; }
    public bool? Force3DS { get; set; }
    public List<InstallmentDetailDto> Details { get; set; } = new();
}

public class InstallmentDetailDto
{
    public int InstallmentNumber { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal InstallmentPrice { get; set; }
    public decimal? InstallmentRate { get; set; }
}

public class ThreeDSCallbackDto
{
    public string? Status { get; set; }
    public string? PaymentId { get; set; }
    public string? ConversationId { get; set; }
    public string? MdStatus { get; set; }
}

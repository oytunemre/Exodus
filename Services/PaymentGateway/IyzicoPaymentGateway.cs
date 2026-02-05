using FarmazonDemo.Models;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FarmazonDemo.Services.PaymentGateway;

public class IyzicoPaymentGateway : IPaymentGateway
{
    private readonly IyzicoSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<IyzicoPaymentGateway> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public string ProviderName => "iyzico";

    public IyzicoPaymentGateway(
        IOptions<IyzicoSettings> settings,
        HttpClient httpClient,
        ILogger<IyzicoPaymentGateway> logger)
    {
        _settings = settings.Value;
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        try
        {
            var iyzicoRequest = BuildPaymentRequest(request);
            var response = await SendRequestAsync<IyzicoPaymentResponse>("/payment/auth", iyzicoRequest, ct);

            return new PaymentInitiationResult
            {
                Success = response.Status == "success",
                PaymentId = response.PaymentId,
                PaymentTransactionId = response.ItemTransactions?.FirstOrDefault()?.PaymentTransactionId,
                CardLast4 = response.CardLast4,
                CardBrand = response.CardAssociation,
                PaidPrice = ParseDecimal(response.PaidPrice),
                Currency = response.Currency,
                Installment = response.Installment,
                ErrorCode = response.ErrorCode,
                ErrorMessage = response.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment");
            return new PaymentInitiationResult
            {
                Success = false,
                ErrorCode = "GATEWAY_ERROR",
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<Payment3DSResult> Initiate3DSPaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        try
        {
            var iyzicoRequest = BuildPaymentRequest(request);
            iyzicoRequest["callbackUrl"] = request.CallbackUrl ?? _settings.ThreeDSCallbackUrl;

            var response = await SendRequestAsync<Iyzico3DSInitResponse>("/payment/3dsecure/initialize", iyzicoRequest, ct);

            return new Payment3DSResult
            {
                Success = response.Status == "success",
                PaymentId = response.ConversationId,
                ThreeDSHtmlContent = response.ThreeDSHtmlContent,
                ErrorCode = response.ErrorCode,
                ErrorMessage = response.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating 3DS payment");
            return new Payment3DSResult
            {
                Success = false,
                ErrorCode = "GATEWAY_ERROR",
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PaymentResult> CompletePaymentAsync(string paymentToken, CancellationToken ct = default)
    {
        try
        {
            var request = new Dictionary<string, object>
            {
                ["locale"] = "tr",
                ["conversationId"] = paymentToken,
                ["paymentId"] = paymentToken
            };

            var response = await SendRequestAsync<IyzicoPaymentResponse>("/payment/detail", request, ct);

            return MapToPaymentResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing payment");
            return new PaymentResult
            {
                Success = false,
                ErrorCode = "GATEWAY_ERROR",
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PaymentResult> Complete3DSPaymentAsync(string paymentToken, CancellationToken ct = default)
    {
        try
        {
            var request = new Dictionary<string, object>
            {
                ["locale"] = "tr",
                ["conversationId"] = Guid.NewGuid().ToString(),
                ["paymentId"] = paymentToken
            };

            var response = await SendRequestAsync<IyzicoPaymentResponse>("/payment/3dsecure/auth", request, ct);

            return MapToPaymentResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing 3DS payment");
            return new PaymentResult
            {
                Success = false,
                ErrorCode = "GATEWAY_ERROR",
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<RefundResult> RefundAsync(string paymentTransactionId, decimal? amount = null, CancellationToken ct = default)
    {
        try
        {
            var request = new Dictionary<string, object>
            {
                ["locale"] = "tr",
                ["conversationId"] = Guid.NewGuid().ToString(),
                ["paymentTransactionId"] = paymentTransactionId,
                ["ip"] = "85.34.78.112"
            };

            if (amount.HasValue)
            {
                request["price"] = amount.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            }

            var response = await SendRequestAsync<IyzicoRefundResponse>("/payment/refund", request, ct);

            return new RefundResult
            {
                Success = response.Status == "success",
                PaymentId = response.PaymentId,
                PaymentTransactionId = response.PaymentTransactionId,
                RefundedAmount = ParseDecimal(response.Price),
                Currency = response.Currency,
                ErrorCode = response.ErrorCode,
                ErrorMessage = response.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund");
            return new RefundResult
            {
                Success = false,
                ErrorCode = "GATEWAY_ERROR",
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(string paymentTransactionId, CancellationToken ct = default)
    {
        try
        {
            var request = new Dictionary<string, object>
            {
                ["locale"] = "tr",
                ["conversationId"] = Guid.NewGuid().ToString(),
                ["paymentId"] = paymentTransactionId
            };

            var response = await SendRequestAsync<IyzicoPaymentResponse>("/payment/detail", request, ct);

            return new PaymentStatusResult
            {
                Success = response.Status == "success",
                PaymentId = response.PaymentId,
                Status = response.Status,
                PaidPrice = ParseDecimal(response.PaidPrice),
                Currency = response.Currency,
                ErrorCode = response.ErrorCode,
                ErrorMessage = response.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status");
            return new PaymentStatusResult
            {
                Success = false,
                ErrorCode = "GATEWAY_ERROR",
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<BinCheckResult> CheckBinAsync(string binNumber, CancellationToken ct = default)
    {
        try
        {
            var cleanBin = binNumber?.Replace(" ", "").Replace("-", "") ?? "";
            if (cleanBin.Length < 6)
            {
                return new BinCheckResult
                {
                    Success = false,
                    ErrorCode = "INVALID_BIN",
                    ErrorMessage = "BIN number must be at least 6 digits"
                };
            }

            var request = new Dictionary<string, object>
            {
                ["locale"] = "tr",
                ["conversationId"] = Guid.NewGuid().ToString(),
                ["binNumber"] = cleanBin[..6]
            };

            var response = await SendRequestAsync<IyzicoBinCheckResponse>("/payment/bin/check", request, ct);

            return new BinCheckResult
            {
                Success = response.Status == "success",
                BinNumber = response.BinNumber,
                CardType = response.CardType,
                CardAssociation = response.CardAssociation,
                CardFamily = response.CardFamily,
                BankName = response.BankName,
                BankCode = response.BankCode,
                Commercial = response.Commercial == 1,
                ErrorCode = response.ErrorCode,
                ErrorMessage = response.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking bin");
            return new BinCheckResult
            {
                Success = false,
                ErrorCode = "GATEWAY_ERROR",
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<InstallmentResult> GetInstallmentOptionsAsync(string binNumber, decimal price, CancellationToken ct = default)
    {
        try
        {
            var cleanBin = binNumber?.Replace(" ", "").Replace("-", "") ?? "";
            if (cleanBin.Length < 6)
            {
                return new InstallmentResult
                {
                    Success = false,
                    ErrorCode = "INVALID_BIN",
                    ErrorMessage = "BIN number must be at least 6 digits"
                };
            }

            var request = new Dictionary<string, object>
            {
                ["locale"] = "tr",
                ["conversationId"] = Guid.NewGuid().ToString(),
                ["binNumber"] = cleanBin[..6],
                ["price"] = price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
            };

            var response = await SendRequestAsync<IyzicoInstallmentResponse>("/payment/iyzipos/installment", request, ct);

            var result = new InstallmentResult
            {
                Success = response.Status == "success",
                ErrorCode = response.ErrorCode,
                ErrorMessage = response.ErrorMessage
            };

            if (response.InstallmentDetails != null)
            {
                foreach (var detail in response.InstallmentDetails)
                {
                    var option = new InstallmentOption
                    {
                        BankName = detail.BankName,
                        BankCode = detail.BankCode,
                        CardType = detail.CardType,
                        CardAssociation = detail.CardAssociation,
                        CardFamily = detail.CardFamilyName,
                        Force3DS = detail.Force3Ds == 1
                    };

                    if (detail.InstallmentPrices != null)
                    {
                        foreach (var installment in detail.InstallmentPrices)
                        {
                            option.Details.Add(new InstallmentDetail
                            {
                                InstallmentNumber = installment.InstallmentNumber ?? 1,
                                TotalPrice = ParseDecimal(installment.TotalPrice) ?? price,
                                InstallmentPrice = ParseDecimal(installment.InstallmentPrice) ?? price,
                                InstallmentRate = ParseDecimal(installment.InstallmentRate)
                            });
                        }
                    }

                    result.Options.Add(option);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting installment options");
            return new InstallmentResult
            {
                Success = false,
                ErrorCode = "GATEWAY_ERROR",
                ErrorMessage = ex.Message
            };
        }
    }

    public bool ValidateWebhookSignature(string payload, string signature)
    {
        if (string.IsNullOrEmpty(signature)) return false;

        var expectedSignature = GenerateSignature(payload);
        return string.Equals(expectedSignature, signature, StringComparison.OrdinalIgnoreCase);
    }

    private Dictionary<string, object> BuildPaymentRequest(PaymentRequest request)
    {
        var iyzicoRequest = new Dictionary<string, object>
        {
            ["locale"] = "tr",
            ["conversationId"] = request.ConversationId,
            ["price"] = request.Price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            ["paidPrice"] = request.PaidPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            ["currency"] = request.Currency,
            ["installment"] = request.InstallmentCount ?? 1,
            ["basketId"] = request.BasketId ?? Guid.NewGuid().ToString(),
            ["paymentChannel"] = "WEB",
            ["paymentGroup"] = "PRODUCT"
        };

        if (request.Card != null)
        {
            if (!string.IsNullOrEmpty(request.Card.CardToken))
            {
                iyzicoRequest["paymentCard"] = new Dictionary<string, object>
                {
                    ["cardToken"] = request.Card.CardToken,
                    ["cardUserKey"] = request.Card.CardUserKey ?? ""
                };
            }
            else
            {
                iyzicoRequest["paymentCard"] = new Dictionary<string, object>
                {
                    ["cardHolderName"] = request.Card.CardHolderName,
                    ["cardNumber"] = request.Card.CardNumber.Replace(" ", ""),
                    ["expireMonth"] = request.Card.ExpireMonth,
                    ["expireYear"] = request.Card.ExpireYear,
                    ["cvc"] = request.Card.Cvc,
                    ["registerCard"] = request.Card.RegisterCard ? 1 : 0
                };
            }
        }

        iyzicoRequest["buyer"] = new Dictionary<string, object>
        {
            ["id"] = request.Buyer.Id,
            ["name"] = request.Buyer.Name,
            ["surname"] = request.Buyer.Surname,
            ["gsmNumber"] = request.Buyer.Phone,
            ["email"] = request.Buyer.Email,
            ["identityNumber"] = request.Buyer.IdentityNumber,
            ["lastLoginDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            ["registrationDate"] = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd HH:mm:ss"),
            ["registrationAddress"] = request.Buyer.RegistrationAddress,
            ["ip"] = request.Buyer.Ip,
            ["city"] = request.Buyer.City,
            ["country"] = request.Buyer.Country
        };

        iyzicoRequest["shippingAddress"] = new Dictionary<string, object>
        {
            ["contactName"] = request.ShippingAddress.ContactName,
            ["city"] = request.ShippingAddress.City,
            ["country"] = request.ShippingAddress.Country,
            ["address"] = request.ShippingAddress.Address,
            ["zipCode"] = request.ShippingAddress.ZipCode ?? ""
        };

        iyzicoRequest["billingAddress"] = new Dictionary<string, object>
        {
            ["contactName"] = request.BillingAddress.ContactName,
            ["city"] = request.BillingAddress.City,
            ["country"] = request.BillingAddress.Country,
            ["address"] = request.BillingAddress.Address,
            ["zipCode"] = request.BillingAddress.ZipCode ?? ""
        };

        var basketItems = request.BasketItems.Select(item => new Dictionary<string, object>
        {
            ["id"] = item.Id,
            ["name"] = item.Name,
            ["category1"] = item.Category1,
            ["category2"] = item.Category2 ?? "",
            ["itemType"] = item.ItemType,
            ["price"] = item.Price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
        }).ToList();

        iyzicoRequest["basketItems"] = basketItems;

        return iyzicoRequest;
    }

    private async Task<T> SendRequestAsync<T>(string endpoint, Dictionary<string, object> request, CancellationToken ct)
    {
        var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
        var randomString = GenerateRandomString(8);
        var authorizationString = GenerateAuthorizationString(endpoint, jsonContent, randomString);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        httpRequest.Headers.Add("Authorization", authorizationString);
        httpRequest.Headers.Add("x-iyzi-rnd", randomString);

        _logger.LogDebug("Sending request to {Endpoint}", endpoint);

        var response = await _httpClient.SendAsync(httpRequest, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        _logger.LogDebug("Response from {Endpoint}: {Response}", endpoint, responseContent);

        var result = JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
        return result ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    private string GenerateAuthorizationString(string endpoint, string jsonContent, string randomString)
    {
        var hashString = $"{_settings.ApiKey}{randomString}{_settings.SecretKey}{jsonContent}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashString));
        var hash = Convert.ToBase64String(hashBytes);

        var authorizationString = $"apiKey:{_settings.ApiKey}&randomKey:{randomString}&signature:{hash}";
        return $"IYZWS {Convert.ToBase64String(Encoding.UTF8.GetBytes(authorizationString))}";
    }

    private string GenerateSignature(string payload)
    {
        var hashString = $"{_settings.SecretKey}{payload}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashString));
        return Convert.ToBase64String(hashBytes);
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static PaymentResult MapToPaymentResult(IyzicoPaymentResponse response)
    {
        return new PaymentResult
        {
            Success = response.Status == "success",
            PaymentId = response.PaymentId,
            PaymentTransactionId = response.ItemTransactions?.FirstOrDefault()?.PaymentTransactionId,
            PaidPrice = ParseDecimal(response.PaidPrice),
            Currency = response.Currency,
            Installment = response.Installment,
            CardLast4 = response.CardLast4,
            CardBrand = response.CardAssociation,
            CardAssociation = response.CardAssociation,
            CardFamily = response.CardFamily,
            CardType = response.CardType,
            FraudStatus = response.FraudStatus?.ToString(),
            ErrorCode = response.ErrorCode,
            ErrorMessage = response.ErrorMessage,
            AuthCode = response.AuthCode,
            HostReference = response.HostReference
        };
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return decimal.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : null;
    }
}

#region Iyzico Response Models

internal class IyzicoBaseResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("errorGroup")]
    public string? ErrorGroup { get; set; }

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("systemTime")]
    public long? SystemTime { get; set; }

    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; set; }
}

internal class IyzicoPaymentResponse : IyzicoBaseResponse
{
    [JsonPropertyName("paymentId")]
    public string? PaymentId { get; set; }

    [JsonPropertyName("price")]
    public string? Price { get; set; }

    [JsonPropertyName("paidPrice")]
    public string? PaidPrice { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("installment")]
    public int? Installment { get; set; }

    [JsonPropertyName("basketId")]
    public string? BasketId { get; set; }

    [JsonPropertyName("binNumber")]
    public string? BinNumber { get; set; }

    [JsonPropertyName("lastFourDigits")]
    public string? CardLast4 { get; set; }

    [JsonPropertyName("cardType")]
    public string? CardType { get; set; }

    [JsonPropertyName("cardAssociation")]
    public string? CardAssociation { get; set; }

    [JsonPropertyName("cardFamily")]
    public string? CardFamily { get; set; }

    [JsonPropertyName("fraudStatus")]
    public int? FraudStatus { get; set; }

    [JsonPropertyName("authCode")]
    public string? AuthCode { get; set; }

    [JsonPropertyName("hostReference")]
    public string? HostReference { get; set; }

    [JsonPropertyName("itemTransactions")]
    public List<IyzicoItemTransaction>? ItemTransactions { get; set; }
}

internal class IyzicoItemTransaction
{
    [JsonPropertyName("itemId")]
    public string? ItemId { get; set; }

    [JsonPropertyName("paymentTransactionId")]
    public string? PaymentTransactionId { get; set; }

    [JsonPropertyName("transactionStatus")]
    public int? TransactionStatus { get; set; }

    [JsonPropertyName("price")]
    public string? Price { get; set; }

    [JsonPropertyName("paidPrice")]
    public string? PaidPrice { get; set; }
}

internal class Iyzico3DSInitResponse : IyzicoBaseResponse
{
    [JsonPropertyName("threeDSHtmlContent")]
    public string? ThreeDSHtmlContent { get; set; }
}

internal class IyzicoRefundResponse : IyzicoBaseResponse
{
    [JsonPropertyName("paymentId")]
    public string? PaymentId { get; set; }

    [JsonPropertyName("paymentTransactionId")]
    public string? PaymentTransactionId { get; set; }

    [JsonPropertyName("price")]
    public string? Price { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}

internal class IyzicoBinCheckResponse : IyzicoBaseResponse
{
    [JsonPropertyName("binNumber")]
    public string? BinNumber { get; set; }

    [JsonPropertyName("cardType")]
    public string? CardType { get; set; }

    [JsonPropertyName("cardAssociation")]
    public string? CardAssociation { get; set; }

    [JsonPropertyName("cardFamily")]
    public string? CardFamily { get; set; }

    [JsonPropertyName("bankName")]
    public string? BankName { get; set; }

    [JsonPropertyName("bankCode")]
    public long? BankCode { get; set; }

    [JsonPropertyName("commercial")]
    public int? Commercial { get; set; }
}

internal class IyzicoInstallmentResponse : IyzicoBaseResponse
{
    [JsonPropertyName("installmentDetails")]
    public List<IyzicoInstallmentDetail>? InstallmentDetails { get; set; }
}

internal class IyzicoInstallmentDetail
{
    [JsonPropertyName("binNumber")]
    public string? BinNumber { get; set; }

    [JsonPropertyName("price")]
    public string? Price { get; set; }

    [JsonPropertyName("cardType")]
    public string? CardType { get; set; }

    [JsonPropertyName("cardAssociation")]
    public string? CardAssociation { get; set; }

    [JsonPropertyName("cardFamilyName")]
    public string? CardFamilyName { get; set; }

    [JsonPropertyName("bankName")]
    public string? BankName { get; set; }

    [JsonPropertyName("bankCode")]
    public long? BankCode { get; set; }

    [JsonPropertyName("force3ds")]
    public int? Force3Ds { get; set; }

    [JsonPropertyName("installmentPrices")]
    public List<IyzicoInstallmentPrice>? InstallmentPrices { get; set; }
}

internal class IyzicoInstallmentPrice
{
    [JsonPropertyName("installmentPrice")]
    public string? InstallmentPrice { get; set; }

    [JsonPropertyName("totalPrice")]
    public string? TotalPrice { get; set; }

    [JsonPropertyName("installmentNumber")]
    public int? InstallmentNumber { get; set; }

    [JsonPropertyName("installmentRate")]
    public string? InstallmentRate { get; set; }
}

#endregion

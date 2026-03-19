namespace Exodus.Services.PaymentGateway;

public class MockPaymentGateway : IPaymentGateway
{
    public string ProviderName => "MOCK";

    public Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentRequest request, CancellationToken ct = default) =>
        Task.FromResult(new PaymentInitiationResult
        {
            Success = true,
            PaymentId = Guid.NewGuid().ToString(),
            PaymentTransactionId = Guid.NewGuid().ToString(),
            CardLast4 = "0000",
            CardBrand = "Mock",
            PaidPrice = request.PaidPrice,
            Currency = request.Currency
        });

    public Task<PaymentResult> CompletePaymentAsync(string paymentToken, CancellationToken ct = default) =>
        Task.FromResult(new PaymentResult
        {
            Success = true,
            PaymentId = paymentToken,
            PaymentTransactionId = Guid.NewGuid().ToString(),
            CardLast4 = "0000",
            CardBrand = "Mock"
        });

    public Task<Payment3DSResult> Initiate3DSPaymentAsync(PaymentRequest request, CancellationToken ct = default) =>
        Task.FromResult(new Payment3DSResult
        {
            Success = true,
            PaymentId = Guid.NewGuid().ToString(),
            ThreeDSHtmlContent = "<html><body>Mock 3DS</body></html>"
        });

    public Task<PaymentResult> Complete3DSPaymentAsync(string paymentToken, CancellationToken ct = default) =>
        Task.FromResult(new PaymentResult
        {
            Success = true,
            PaymentId = paymentToken,
            PaymentTransactionId = Guid.NewGuid().ToString(),
            CardLast4 = "0000",
            CardBrand = "Mock"
        });

    public Task<RefundResult> RefundAsync(string paymentTransactionId, decimal? amount = null, CancellationToken ct = default) =>
        Task.FromResult(new RefundResult
        {
            Success = true,
            PaymentId = paymentTransactionId,
            PaymentTransactionId = Guid.NewGuid().ToString(),
            RefundedAmount = amount,
            Currency = "TRY"
        });

    public Task<PaymentStatusResult> GetPaymentStatusAsync(string paymentTransactionId, CancellationToken ct = default) =>
        Task.FromResult(new PaymentStatusResult
        {
            Success = true,
            PaymentId = paymentTransactionId,
            Status = "completed",
            Currency = "TRY"
        });

    public Task<BinCheckResult> CheckBinAsync(string binNumber, CancellationToken ct = default) =>
        Task.FromResult(new BinCheckResult
        {
            Success = true,
            BinNumber = binNumber,
            CardType = "CREDIT_CARD",
            CardAssociation = "VISA",
            CardFamily = "Classic",
            BankName = "Mock Bank"
        });

    public Task<InstallmentResult> GetInstallmentOptionsAsync(string binNumber, decimal price, CancellationToken ct = default) =>
        Task.FromResult(new InstallmentResult
        {
            Success = true,
            Options = new List<InstallmentOption>()
        });

    public bool ValidateWebhookSignature(string payload, string signature) => true;
}

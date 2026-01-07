using FarmazonDemo.Models.Dto.Payment;

namespace FarmazonDemo.Services.Payments;

public interface IPaymentService
{
    Task<PaymentIntentResponseDto> CreateIntentAsync(CreatePaymentIntentDto dto, CancellationToken ct = default);
    Task<PaymentIntentResponseDto> GetByOrderIdAsync(int orderId, CancellationToken ct = default);
    Task<PaymentIntentResponseDto> MarkReceivedAsync(int paymentIntentId, string? note, CancellationToken ct = default);
    Task<PaymentIntentResponseDto> SimulateSuccessAsync(int paymentIntentId, string? note, CancellationToken ct = default);
    Task<PaymentIntentResponseDto> SimulateFailAsync(int paymentIntentId, string? reason, CancellationToken ct = default);
}

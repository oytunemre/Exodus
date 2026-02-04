using FarmazonDemo.Models.Dto.Payment;
using FarmazonDemo.Services.Payments;
using FarmazonDemo.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FarmazonDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// Create payment intent for order
    /// </summary>
    [HttpPost("intents")]
    public async Task<ActionResult<PaymentIntentResponseDto>> CreateIntent([FromBody] CreatePaymentIntentDto dto, CancellationToken ct)
    {
        var result = await _paymentService.CreateIntentAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get payment intent by ID
    /// </summary>
    [HttpGet("intents/{id:int}")]
    public async Task<ActionResult<PaymentIntentResponseDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _paymentService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get payment intent by order ID
    /// </summary>
    [HttpGet("order/{orderId:int}")]
    public async Task<ActionResult<PaymentIntentResponseDto>> GetByOrderId(int orderId, CancellationToken ct)
    {
        var result = await _paymentService.GetByOrderIdAsync(orderId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Authorize payment (pre-capture)
    /// </summary>
    [HttpPost("intents/{id:int}/authorize")]
    public async Task<ActionResult<PaymentIntentResponseDto>> Authorize(int id, CancellationToken ct)
    {
        var result = await _paymentService.AuthorizeAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Capture authorized payment
    /// </summary>
    [HttpPost("intents/{id:int}/capture")]
    public async Task<ActionResult<PaymentIntentResponseDto>> Capture(int id, [FromBody] CapturePaymentDto? dto, CancellationToken ct)
    {
        var result = await _paymentService.CaptureAsync(id, dto?.Note, ct);
        return Ok(result);
    }

    /// <summary>
    /// Cancel payment intent
    /// </summary>
    [HttpPost("intents/{id:int}/cancel")]
    public async Task<ActionResult<PaymentIntentResponseDto>> Cancel(int id, [FromBody] CancelPaymentDto? dto, CancellationToken ct)
    {
        var result = await _paymentService.CancelAsync(id, dto?.Reason, ct);
        return Ok(result);
    }

    /// <summary>
    /// Request refund for payment
    /// </summary>
    [HttpPost("intents/{id:int}/refund")]
    public async Task<ActionResult<RefundPaymentResponseDto>> Refund(int id, [FromBody] RefundPaymentDto dto, CancellationToken ct)
    {
        var result = await _paymentService.RefundAsync(id, dto.Amount, dto.Reason, ct);
        return Ok(result);
    }

    /// <summary>
    /// Confirm 3D Secure authentication
    /// </summary>
    [HttpPost("intents/{id:int}/3ds/confirm")]
    public async Task<ActionResult<PaymentIntentResponseDto>> Confirm3DSecure(int id, [FromBody] Confirm3DSecureDto dto, CancellationToken ct)
    {
        var result = await _paymentService.Confirm3DSecureAsync(id, dto.AuthenticationResult, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get payment events/history
    /// </summary>
    [HttpGet("intents/{id:int}/events")]
    public async Task<ActionResult<List<PaymentEventDto>>> GetEvents(int id, CancellationToken ct)
    {
        var result = await _paymentService.GetPaymentEventsAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Mark payment as received (manual/admin)
    /// </summary>
    [HttpPost("intents/{id:int}/mark-received")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PaymentIntentResponseDto>> MarkReceived(int id, [FromBody] MarkPaymentReceivedDto? dto, CancellationToken ct)
    {
        var result = await _paymentService.MarkReceivedAsync(id, dto?.Note, ct);
        return Ok(result);
    }

    /// <summary>
    /// Simulate payment success (testing only)
    /// </summary>
    [HttpPost("intents/{id:int}/simulate-success")]
    public async Task<ActionResult<PaymentIntentResponseDto>> SimulateSuccess(int id, [FromBody] SimulatePaymentDto? dto, CancellationToken ct)
    {
        var result = await _paymentService.SimulateSuccessAsync(id, dto?.Reason, ct);
        return Ok(result);
    }

    /// <summary>
    /// Simulate payment failure (testing only)
    /// </summary>
    [HttpPost("intents/{id:int}/simulate-fail")]
    public async Task<ActionResult<PaymentIntentResponseDto>> SimulateFail(int id, [FromBody] SimulatePaymentDto? dto, CancellationToken ct)
    {
        var result = await _paymentService.SimulateFailAsync(id, dto?.Reason, ct);
        return Ok(result);
    }

    /// <summary>
    /// Webhook endpoint for payment provider callbacks
    /// </summary>
    [HttpPost("webhook/{provider}")]
    [AllowAnonymous]
    public async Task<ActionResult> Webhook(string provider, [FromBody] ProcessWebhookDto dto, CancellationToken ct)
    {
        var signature = Request.Headers["X-Webhook-Signature"].FirstOrDefault();
        await _paymentService.ProcessWebhookAsync(provider, dto.Payload, signature, ct);
        return Ok(new { received = true });
    }

    // =====================================================
    // IYZICO GATEWAY ENDPOINTS
    // =====================================================

    /// <summary>
    /// Process payment directly via iyzico gateway (non-3DS)
    /// </summary>
    [HttpPost("gateway/process")]
    public async Task<ActionResult<IyzicoPaymentResponseDto>> ProcessWithGateway([FromBody] ProcessGatewayPaymentDto dto, CancellationToken ct)
    {
        dto.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _paymentService.ProcessWithGatewayAsync(dto, ct);
        return Ok(result);
    }

    /// <summary>
    /// Initialize 3D Secure payment via iyzico
    /// </summary>
    [HttpPost("gateway/3ds/initialize")]
    public async Task<ActionResult<Iyzico3DSResponseDto>> Initialize3DSPayment([FromBody] ProcessGatewayPaymentDto dto, CancellationToken ct)
    {
        dto.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _paymentService.Initialize3DSPaymentAsync(dto, ct);
        return Ok(result);
    }

    /// <summary>
    /// Complete 3D Secure payment after bank redirect
    /// </summary>
    [HttpPost("gateway/3ds/complete")]
    [AllowAnonymous]
    public async Task<ActionResult<IyzicoPaymentResponseDto>> Complete3DSPayment([FromForm] ThreeDSCallbackDto callback, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(callback.PaymentId))
            return BadRequest(new { error = "PaymentId is required" });

        var result = await _paymentService.Complete3DSPaymentAsync(callback.PaymentId, ct);

        // Redirect to frontend with result
        if (result.Success)
        {
            return Redirect($"/payment/success?paymentId={result.PaymentIntentId}");
        }
        else
        {
            return Redirect($"/payment/failed?error={result.ErrorMessage}");
        }
    }

    /// <summary>
    /// Check BIN number to get card details
    /// </summary>
    [HttpGet("gateway/bin/{binNumber}")]
    public async Task<ActionResult<BinCheckResponseDto>> CheckBin(string binNumber, CancellationToken ct)
    {
        var result = await _paymentService.CheckBinAsync(binNumber, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get available installment options for a BIN and price
    /// </summary>
    [HttpGet("gateway/installments")]
    public async Task<ActionResult<InstallmentResponseDto>> GetInstallmentOptions(
        [FromQuery] string binNumber,
        [FromQuery] decimal price,
        CancellationToken ct)
    {
        var result = await _paymentService.GetInstallmentOptionsAsync(binNumber, price, ct);
        return Ok(result);
    }

    /// <summary>
    /// 3D Secure callback endpoint (called by iyzico after bank auth)
    /// </summary>
    [HttpPost("3ds-callback")]
    [AllowAnonymous]
    public async Task<ActionResult> ThreeDSCallback([FromForm] ThreeDSCallbackDto callback, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(callback.PaymentId))
            return BadRequest("Invalid callback");

        var result = await _paymentService.Complete3DSPaymentAsync(callback.PaymentId, ct);

        // Return HTML page that posts message to parent window (for iframe integration)
        var html = $@"
<!DOCTYPE html>
<html>
<head><title>Odeme Sonucu</title></head>
<body>
<script>
    window.parent.postMessage({{
        type: '3ds_complete',
        success: {result.Success.ToString().ToLower()},
        paymentId: '{result.PaymentIntentId}',
        errorMessage: '{result.ErrorMessage ?? ""}'
    }}, '*');
</script>
<p>{(result.Success ? "Odeme basarili! Yonlendiriliyorsunuz..." : $"Odeme basarisiz: {result.ErrorMessage}")}</p>
</body>
</html>";

        return Content(html, "text/html");
    }
}

// Additional DTOs for controller actions
public class CapturePaymentDto
{
    public string? Note { get; set; }
}

public class CancelPaymentDto
{
    public string? Reason { get; set; }
}

public class Confirm3DSecureDto
{
    public required string AuthenticationResult { get; set; }
}

public class MarkPaymentReceivedDto
{
    public string? Note { get; set; }
}

public class SimulatePaymentDto
{
    public string? Reason { get; set; }
}

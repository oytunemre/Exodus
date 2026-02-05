using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto.Payment;
using FarmazonDemo.Models.Enums;
using FarmazonDemo.Services.Common;
using FarmazonDemo.Services.Notifications;
using FarmazonDemo.Services.Payments;
using FarmazonDemo.Tests.Mocks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FarmazonDemo.Tests.Unit.Services;

public class PaymentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;
    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext(Guid.NewGuid().ToString());
        TestDbContextFactory.SeedPaymentTestData(_context);
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<PaymentService>>();
        _paymentService = new PaymentService(_context, _notificationServiceMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region CreateIntent Tests

    [Fact]
    public async Task CreateIntentAsync_ValidOrder_CreatesPaymentIntent()
    {
        // Arrange - Create new order without existing payment intent
        var newOrder = new Models.Entities.Order
        {
            Id = 2,
            OrderNumber = "ORD-20260205-TEST0002",
            BuyerId = 1,
            Status = OrderStatus.Pending,
            TotalAmount = 500.00m
        };
        _context.Orders.Add(newOrder);
        await _context.SaveChangesAsync();

        var dto = new CreatePaymentIntentDto
        {
            OrderId = 2,
            Currency = "TRY",
            Method = PaymentMethod.CreditCard
        };

        // Act
        var result = await _paymentService.CreateIntentAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(2);
        result.Amount.Should().Be(500.00m);
        result.Currency.Should().Be("TRY");
        result.Status.Should().Be(PaymentStatus.Created);
    }

    [Fact]
    public async Task CreateIntentAsync_ExistingPaymentIntent_ReturnsExisting()
    {
        // Arrange - Order 1 already has payment intent
        var dto = new CreatePaymentIntentDto
        {
            OrderId = 1,
            Currency = "TRY",
            Method = PaymentMethod.CreditCard
        };

        // Act
        var result = await _paymentService.CreateIntentAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1); // Should return existing payment intent
    }

    [Fact]
    public async Task CreateIntentAsync_NonExistingOrder_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new CreatePaymentIntentDto
        {
            OrderId = 999,
            Currency = "TRY",
            Method = PaymentMethod.CreditCard
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _paymentService.CreateIntentAsync(dto));
    }

    [Fact]
    public async Task CreateIntentAsync_AmountOver500_Requires3DSecure()
    {
        // Arrange
        var newOrder = new Models.Entities.Order
        {
            Id = 3,
            OrderNumber = "ORD-20260205-TEST0003",
            BuyerId = 1,
            Status = OrderStatus.Pending,
            TotalAmount = 600.00m // Over 500
        };
        _context.Orders.Add(newOrder);
        await _context.SaveChangesAsync();

        var dto = new CreatePaymentIntentDto
        {
            OrderId = 3,
            Currency = "TRY",
            Method = PaymentMethod.CreditCard,
            CardDetails = new CardDetailsDto
            {
                CardNumber = "4111111111111111",
                CardHolderName = "Test User",
                ExpireMonth = "12",
                ExpireYear = "2028",
                Cvc = "123"
            },
            ReturnUrl = "https://example.com/return"
        };

        // Act
        var result = await _paymentService.CreateIntentAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Requires3DSecure.Should().BeTrue();
        result.Status.Should().Be(PaymentStatus.Pending);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetByIdAsync_ExistingPaymentIntent_ReturnsPaymentIntent()
    {
        // Arrange
        var paymentIntentId = 1;

        // Act
        var result = await _paymentService.GetByIdAsync(paymentIntentId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(paymentIntentId);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingPaymentIntent_ThrowsNotFoundException()
    {
        // Arrange
        var paymentIntentId = 999;

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _paymentService.GetByIdAsync(paymentIntentId));
    }

    #endregion

    #region Status Transitions Tests

    [Fact]
    public async Task AuthorizeAsync_ValidPaymentIntent_AuthorizesPayment()
    {
        // Arrange
        var paymentIntentId = 1;

        // Act
        var result = await _paymentService.AuthorizeAsync(paymentIntentId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Authorized);
        result.AuthorizedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CaptureAsync_AuthorizedPayment_CapturesPayment()
    {
        // Arrange
        var paymentIntentId = 1;
        await _paymentService.AuthorizeAsync(paymentIntentId); // First authorize

        // Act
        var result = await _paymentService.CaptureAsync(paymentIntentId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Captured);
        result.CapturedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CaptureAsync_CreatedPayment_CapturesPayment()
    {
        // Arrange
        var paymentIntentId = 1; // Status is Created

        // Act
        var result = await _paymentService.CaptureAsync(paymentIntentId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Captured);
    }

    [Fact]
    public async Task CancelAsync_PendingPayment_CancelsPayment()
    {
        // Arrange
        var paymentIntentId = 1;

        // Act
        var result = await _paymentService.CancelAsync(paymentIntentId, "Test cancellation");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Cancelled);
        result.FailureReason.Should().Be("Test cancellation");
    }

    [Fact]
    public async Task FailAsync_PendingPayment_FailsPayment()
    {
        // Arrange
        var paymentIntentId = 1;

        // Act
        var result = await _paymentService.FailAsync(paymentIntentId, "Payment declined");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Failed);
        result.FailedAt.Should().NotBeNull();
        result.FailureReason.Should().Be("Payment declined");
    }

    [Fact]
    public async Task CaptureAsync_AlreadyFailed_ThrowsBadRequestException()
    {
        // Arrange
        var paymentIntentId = 1;
        await _paymentService.FailAsync(paymentIntentId, "Failed"); // First fail

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _paymentService.CaptureAsync(paymentIntentId));
    }

    #endregion

    #region Refund Tests

    [Fact]
    public async Task RefundAsync_CapturedPayment_RefundsPayment()
    {
        // Arrange
        var paymentIntentId = 1;
        await _paymentService.CaptureAsync(paymentIntentId); // First capture

        // Act
        var result = await _paymentService.RefundAsync(paymentIntentId, 100.00m, "Customer request");

        // Assert
        result.Should().NotBeNull();
        result.RefundedAmount.Should().Be(100.00m);
        result.Status.Should().Be(PaymentStatus.PartiallyRefunded);
    }

    [Fact]
    public async Task RefundAsync_FullRefund_FullyRefundsPayment()
    {
        // Arrange
        var paymentIntentId = 1;
        await _paymentService.CaptureAsync(paymentIntentId);

        // Act - Refund full amount (210.00)
        var result = await _paymentService.RefundAsync(paymentIntentId, null, "Full refund");

        // Assert
        result.Should().NotBeNull();
        result.RefundedAmount.Should().Be(210.00m);
        result.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public async Task RefundAsync_NotCaptured_ThrowsBadRequestException()
    {
        // Arrange
        var paymentIntentId = 1; // Status is Created, not Captured

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _paymentService.RefundAsync(paymentIntentId, 100.00m));
    }

    [Fact]
    public async Task RefundAsync_ExceedsMaxRefundable_ThrowsBadRequestException()
    {
        // Arrange
        var paymentIntentId = 1;
        await _paymentService.CaptureAsync(paymentIntentId);

        // Act & Assert - Try to refund more than the amount
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _paymentService.RefundAsync(paymentIntentId, 500.00m, "Too much"));
    }

    #endregion

    #region 3D Secure Tests

    [Fact]
    public async Task Confirm3DSecureAsync_SuccessfulAuth_CapturesPayment()
    {
        // Arrange - Create a 3DS pending payment
        var payment = await _context.PaymentIntents.FindAsync(1);
        payment!.Status = PaymentStatus.Pending;
        payment.Requires3DSecure = true;
        await _context.SaveChangesAsync();

        // Act
        var result = await _paymentService.Confirm3DSecureAsync(1, "success");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Captured);
    }

    [Fact]
    public async Task Confirm3DSecureAsync_FailedAuth_FailsPayment()
    {
        // Arrange - Create a 3DS pending payment
        var payment = await _context.PaymentIntents.FindAsync(1);
        payment!.Status = PaymentStatus.Pending;
        payment.Requires3DSecure = true;
        await _context.SaveChangesAsync();

        // Act
        var result = await _paymentService.Confirm3DSecureAsync(1, "failed");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Failed);
        result.FailureReason.Should().Contain("3D Secure");
    }

    [Fact]
    public async Task Confirm3DSecureAsync_Not3DSRequired_ThrowsBadRequestException()
    {
        // Arrange - Payment without 3DS requirement
        var paymentIntentId = 1;

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _paymentService.Confirm3DSecureAsync(paymentIntentId, "success"));
    }

    #endregion

    #region Payment Events Tests

    [Fact]
    public async Task GetPaymentEventsAsync_ExistingPayment_ReturnsEvents()
    {
        // Arrange
        var paymentIntentId = 1;
        await _paymentService.CaptureAsync(paymentIntentId); // This creates events

        // Act
        var result = await _paymentService.GetPaymentEventsAsync(paymentIntentId);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    #endregion

    #region Simulation Tests

    [Fact]
    public async Task SimulateSuccessAsync_ValidPayment_CapturesPayment()
    {
        // Arrange
        var paymentIntentId = 1;

        // Act
        var result = await _paymentService.SimulateSuccessAsync(paymentIntentId, "Test success");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Captured);
    }

    [Fact]
    public async Task SimulateFailAsync_ValidPayment_FailsPayment()
    {
        // Arrange
        var paymentIntentId = 1;

        // Act
        var result = await _paymentService.SimulateFailAsync(paymentIntentId, "Test failure");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Failed);
    }

    #endregion
}

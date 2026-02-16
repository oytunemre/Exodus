using Exodus.Data;
using Exodus.Models.Dto.Payment;
using Exodus.Models.Entities;
using Exodus.Models.Enums;
using Exodus.Services.Common;
using Exodus.Services.Notifications;
using Exodus.Services.Payments;
using Exodus.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Exodus.Tests.Services;

public class PaymentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly PaymentService _service;
    private readonly Mock<INotificationService> _notificationMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;

    public PaymentServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _notificationMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<PaymentService>>();
        _service = new PaymentService(_db, _notificationMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task<Order> SeedOrderAsync(decimal totalAmount = 100m)
    {
        var buyer = new Users
        {
            Name = "Buyer",
            Email = "buyer@test.com",
            Password = "pass",
            Username = "buyer"
        };
        _db.Users.Add(buyer);
        await _db.SaveChangesAsync();

        var order = new Order
        {
            OrderNumber = $"ORD-TEST-{Guid.NewGuid().ToString("N")[..6]}",
            BuyerId = buyer.Id,
            Status = OrderStatus.Pending,
            TotalAmount = totalAmount,
            SubTotal = totalAmount
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    private async Task<PaymentIntent> SeedPaymentIntentAsync(int orderId, PaymentStatus status = PaymentStatus.Created, decimal amount = 100m)
    {
        var intent = new PaymentIntent
        {
            OrderId = orderId,
            Amount = amount,
            Currency = "TRY",
            Method = PaymentMethod.CreditCard,
            Status = status,
            Provider = "STRIPE"
        };
        _db.PaymentIntents.Add(intent);
        await _db.SaveChangesAsync();
        return intent;
    }

    #region CreateIntentAsync

    [Fact]
    public async Task CreateIntentAsync_WithValidOrder_ShouldCreateIntent()
    {
        var order = await SeedOrderAsync(250m);

        var dto = new CreatePaymentIntentDto
        {
            OrderId = order.Id,
            Method = PaymentMethod.CreditCard,
            Currency = "TRY"
        };

        var result = await _service.CreateIntentAsync(dto);

        result.OrderId.Should().Be(order.Id);
        result.Amount.Should().Be(250m);
        result.Status.Should().Be(PaymentStatus.Created);
        result.Currency.Should().Be("TRY");
    }

    [Fact]
    public async Task CreateIntentAsync_WithNonExistentOrder_ShouldThrowNotFoundException()
    {
        var dto = new CreatePaymentIntentDto
        {
            OrderId = 999,
            Method = PaymentMethod.CreditCard
        };

        var act = () => _service.CreateIntentAsync(dto);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateIntentAsync_WhenIntentAlreadyExists_ShouldReturnExisting()
    {
        var order = await SeedOrderAsync(100m);
        var existing = await SeedPaymentIntentAsync(order.Id);

        var dto = new CreatePaymentIntentDto
        {
            OrderId = order.Id,
            Method = PaymentMethod.CreditCard
        };

        var result = await _service.CreateIntentAsync(dto);
        result.Id.Should().Be(existing.Id);
    }

    [Fact]
    public async Task CreateIntentAsync_WithCreditCard_ShouldDetectCardBrand()
    {
        var order = await SeedOrderAsync(100m);

        var dto = new CreatePaymentIntentDto
        {
            OrderId = order.Id,
            Method = PaymentMethod.CreditCard,
            CardDetails = new CardDetailsDto
            {
                CardNumber = "4111111111111111",
                ExpiryDate = "12/25",
                Cvv = "123",
                CardHolderName = "Test User"
            }
        };

        var result = await _service.CreateIntentAsync(dto);
        result.CardBrand.Should().Be("Visa");
        result.CardLast4.Should().Be("1111");
    }

    [Fact]
    public async Task CreateIntentAsync_WithMastercardPrefix_ShouldDetectMastercard()
    {
        var order = await SeedOrderAsync(100m);

        var dto = new CreatePaymentIntentDto
        {
            OrderId = order.Id,
            Method = PaymentMethod.CreditCard,
            CardDetails = new CardDetailsDto
            {
                CardNumber = "5111111111111118",
                ExpiryDate = "12/25",
                Cvv = "123",
                CardHolderName = "Test User"
            }
        };

        var result = await _service.CreateIntentAsync(dto);
        result.CardBrand.Should().Be("Mastercard");
    }

    [Fact]
    public async Task CreateIntentAsync_WithAmountOver500_ShouldRequire3DSecure()
    {
        var order = await SeedOrderAsync(600m);

        var dto = new CreatePaymentIntentDto
        {
            OrderId = order.Id,
            Method = PaymentMethod.CreditCard,
            CardDetails = new CardDetailsDto
            {
                CardNumber = "4111111111111111",
                ExpiryDate = "12/25",
                Cvv = "123",
                CardHolderName = "Test User"
            }
        };

        var result = await _service.CreateIntentAsync(dto);
        result.Requires3DSecure.Should().BeTrue();
        result.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task CreateIntentAsync_WithInstallment_ShouldCalculateInstallmentAmount()
    {
        var order = await SeedOrderAsync(1200m);

        var dto = new CreatePaymentIntentDto
        {
            OrderId = order.Id,
            Method = PaymentMethod.CreditCard,
            InstallmentCount = 6
        };

        var result = await _service.CreateIntentAsync(dto);
        result.InstallmentCount.Should().Be(6);
        result.InstallmentAmount.Should().Be(200m);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenExists_ShouldReturnIntent()
    {
        var order = await SeedOrderAsync();
        var intent = await SeedPaymentIntentAsync(order.Id);

        var result = await _service.GetByIdAsync(intent.Id);
        result.Id.Should().Be(intent.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ShouldThrowNotFoundException()
    {
        var act = () => _service.GetByIdAsync(999);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    #region GetByOrderIdAsync

    [Fact]
    public async Task GetByOrderIdAsync_WhenExists_ShouldReturnIntent()
    {
        var order = await SeedOrderAsync();
        await SeedPaymentIntentAsync(order.Id);

        var result = await _service.GetByOrderIdAsync(order.Id);
        result.OrderId.Should().Be(order.Id);
    }

    [Fact]
    public async Task GetByOrderIdAsync_WhenNotExists_ShouldThrowNotFoundException()
    {
        var act = () => _service.GetByOrderIdAsync(999);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    #region AuthorizeAsync

    [Fact]
    public async Task AuthorizeAsync_FromCreated_ShouldAuthorize()
    {
        var order = await SeedOrderAsync();
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Created);

        var result = await _service.AuthorizeAsync(intent.Id);

        result.Status.Should().Be(PaymentStatus.Authorized);
        result.ExternalReference.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AuthorizeAsync_FromCaptured_ShouldThrowBadRequestException()
    {
        var order = await SeedOrderAsync();
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Captured);

        var act = () => _service.AuthorizeAsync(intent.Id);
        await act.Should().ThrowAsync<BadRequestException>();
    }

    #endregion

    #region CaptureAsync

    [Fact]
    public async Task CaptureAsync_FromAuthorized_ShouldCapture()
    {
        var order = await SeedOrderAsync();
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Authorized);

        var result = await _service.CaptureAsync(intent.Id);

        result.Status.Should().Be(PaymentStatus.Captured);
    }

    [Fact]
    public async Task CaptureAsync_ShouldUpdateOrderStatusToProcessing()
    {
        var order = await SeedOrderAsync();
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Authorized);

        await _service.CaptureAsync(intent.Id);

        var updatedOrder = await _db.Orders.FindAsync(order.Id);
        updatedOrder!.Status.Should().Be(OrderStatus.Processing);
        updatedOrder.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CaptureAsync_ShouldSendNotification()
    {
        var order = await SeedOrderAsync();
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Authorized);

        await _service.CaptureAsync(intent.Id);

        _notificationMock.Verify(n => n.SendPaymentUpdateAsync(
            It.IsAny<int>(), It.IsAny<int>(),
            It.Is<string>(s => s.Contains("Successful")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CaptureAsync_FromFailed_ShouldThrowBadRequestException()
    {
        var order = await SeedOrderAsync();
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Failed);

        var act = () => _service.CaptureAsync(intent.Id);
        await act.Should().ThrowAsync<BadRequestException>();
    }

    #endregion

    #region CancelAsync

    [Fact]
    public async Task CancelAsync_FromCreated_ShouldCancel()
    {
        var order = await SeedOrderAsync();
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Created);

        var result = await _service.CancelAsync(intent.Id, "Customer cancelled");

        result.Status.Should().Be(PaymentStatus.Cancelled);
        result.FailureReason.Should().Be("Customer cancelled");
    }

    [Fact]
    public async Task CancelAsync_FromCaptured_ShouldThrowBadRequestException()
    {
        var order = await SeedOrderAsync();
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Captured);

        var act = () => _service.CancelAsync(intent.Id);
        await act.Should().ThrowAsync<BadRequestException>();
    }

    #endregion

    #region FailAsync

    [Fact]
    public async Task FailAsync_FromCreated_ShouldFail()
    {
        var order = await SeedOrderAsync();
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Created);

        var result = await _service.FailAsync(intent.Id, "Card declined");

        result.Status.Should().Be(PaymentStatus.Failed);
        result.FailureReason.Should().Be("Card declined");
    }

    [Fact]
    public async Task FailAsync_ShouldUpdateOrderToFailed()
    {
        var order = await SeedOrderAsync();
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Created);

        await _service.FailAsync(intent.Id, "Error");

        var updatedOrder = await _db.Orders.FindAsync(order.Id);
        updatedOrder!.Status.Should().Be(OrderStatus.Failed);
    }

    [Fact]
    public async Task FailAsync_ShouldSendNotification()
    {
        var order = await SeedOrderAsync();
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Created);

        await _service.FailAsync(intent.Id, "Insufficient funds");

        _notificationMock.Verify(n => n.SendPaymentUpdateAsync(
            It.IsAny<int>(), It.IsAny<int>(),
            It.Is<string>(s => s.Contains("Failed")),
            It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region RefundAsync

    [Fact]
    public async Task RefundAsync_FullRefund_ShouldRefundEntireAmount()
    {
        var order = await SeedOrderAsync(500m);
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Captured, 500m);

        var result = await _service.RefundAsync(intent.Id);

        result.RefundedAmount.Should().Be(500m);
        result.RemainingAmount.Should().Be(0m);
        result.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public async Task RefundAsync_PartialRefund_ShouldRefundPartialAmount()
    {
        var order = await SeedOrderAsync(500m);
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Captured, 500m);

        var result = await _service.RefundAsync(intent.Id, amount: 200m);

        result.RefundedAmount.Should().Be(200m);
        result.TotalRefundedAmount.Should().Be(200m);
        result.RemainingAmount.Should().Be(300m);
        result.Status.Should().Be(PaymentStatus.PartiallyRefunded);
    }

    [Fact]
    public async Task RefundAsync_MultiplePartialRefunds_ShouldAccumulate()
    {
        var order = await SeedOrderAsync(500m);
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Captured, 500m);

        await _service.RefundAsync(intent.Id, amount: 200m);
        var result = await _service.RefundAsync(intent.Id, amount: 300m);

        result.TotalRefundedAmount.Should().Be(500m);
        result.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public async Task RefundAsync_ExceedingAmount_ShouldThrowBadRequestException()
    {
        var order = await SeedOrderAsync(500m);
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Captured, 500m);

        var act = () => _service.RefundAsync(intent.Id, amount: 600m);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Invalid refund amount*");
    }

    [Fact]
    public async Task RefundAsync_FromNonCapturedStatus_ShouldThrowBadRequestException()
    {
        var order = await SeedOrderAsync();
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Created);

        var act = () => _service.RefundAsync(intent.Id);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Only captured payments*");
    }

    [Fact]
    public async Task RefundAsync_ShouldSendNotification()
    {
        var order = await SeedOrderAsync(100m);
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Captured, 100m);

        await _service.RefundAsync(intent.Id, amount: 50m, reason: "Item damaged");

        _notificationMock.Verify(n => n.SendPaymentUpdateAsync(
            It.IsAny<int>(), It.IsAny<int>(),
            It.Is<string>(s => s.Contains("Refund")),
            It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region Confirm3DSecureAsync

    [Fact]
    public async Task Confirm3DSecureAsync_Success_ShouldAuthorizeAndCapture()
    {
        var order = await SeedOrderAsync(600m);
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Pending, 600m);
        intent.Requires3DSecure = true;
        await _db.SaveChangesAsync();

        var result = await _service.Confirm3DSecureAsync(intent.Id, "success");

        result.Status.Should().Be(PaymentStatus.Captured);
    }

    [Fact]
    public async Task Confirm3DSecureAsync_Failure_ShouldFail()
    {
        var order = await SeedOrderAsync(600m);
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Pending, 600m);
        intent.Requires3DSecure = true;
        await _db.SaveChangesAsync();

        var result = await _service.Confirm3DSecureAsync(intent.Id, "failure");

        result.Status.Should().Be(PaymentStatus.Failed);
        result.FailureReason.Should().Contain("3D Secure");
    }

    [Fact]
    public async Task Confirm3DSecureAsync_WhenNotRequired_ShouldThrowBadRequestException()
    {
        var order = await SeedOrderAsync();
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Pending);

        var act = () => _service.Confirm3DSecureAsync(intent.Id, "success");
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*3D Secure*");
    }

    #endregion

    #region SimulateAsync

    [Fact]
    public async Task SimulateSuccessAsync_ShouldCapturePayment()
    {
        var order = await SeedOrderAsync();
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Created);

        var result = await _service.SimulateSuccessAsync(intent.Id);

        result.Status.Should().Be(PaymentStatus.Captured);
    }

    [Fact]
    public async Task SimulateFailAsync_ShouldFailPayment()
    {
        var order = await SeedOrderAsync();
        var intent = await SeedPaymentIntentAsync(order.Id, PaymentStatus.Created);

        var result = await _service.SimulateFailAsync(intent.Id);

        result.Status.Should().Be(PaymentStatus.Failed);
    }

    #endregion

    #region GetPaymentEventsAsync

    [Fact]
    public async Task GetPaymentEventsAsync_ShouldReturnEventsForPayment()
    {
        var order = await SeedOrderAsync();
        var dto = new CreatePaymentIntentDto
        {
            OrderId = order.Id,
            Method = PaymentMethod.CreditCard
        };

        var intent = await _service.CreateIntentAsync(dto);

        var events = await _service.GetPaymentEventsAsync(intent.Id);
        events.Should().NotBeEmpty();
        events[0].EventType.Should().Be("payment.created");
    }

    #endregion

    #region State Transition Validation

    [Theory]
    [InlineData(PaymentStatus.Created, PaymentStatus.Authorized)]
    [InlineData(PaymentStatus.Created, PaymentStatus.Captured)]
    [InlineData(PaymentStatus.Created, PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Created, PaymentStatus.Cancelled)]
    [InlineData(PaymentStatus.Authorized, PaymentStatus.Captured)]
    [InlineData(PaymentStatus.Authorized, PaymentStatus.Cancelled)]
    public async Task ValidTransitions_ShouldSucceed(PaymentStatus from, PaymentStatus to)
    {
        var order = await SeedOrderAsync();
        var intent = await SeedPaymentIntentAsync(order.Id, from);

        Func<Task> act = to switch
        {
            PaymentStatus.Authorized => () => _service.AuthorizeAsync(intent.Id),
            PaymentStatus.Captured => () => _service.CaptureAsync(intent.Id),
            PaymentStatus.Failed => () => _service.FailAsync(intent.Id),
            PaymentStatus.Cancelled => () => _service.CancelAsync(intent.Id),
            _ => throw new ArgumentException("Unexpected status")
        };

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Cancelled)]
    [InlineData(PaymentStatus.Refunded)]
    public async Task InvalidTransitionToCapture_ShouldThrowBadRequestException(PaymentStatus from)
    {
        var order = await SeedOrderAsync();
        var intent = await SeedPaymentIntentAsync(order.Id, from);

        var act = () => _service.CaptureAsync(intent.Id);
        await act.Should().ThrowAsync<BadRequestException>();
    }

    #endregion
}

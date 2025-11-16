using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Payment.Contract;
using Payment.Model;

namespace Payment.Tests;

using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

[TestFixture]
public class PaymentOnDeliveryStrategyTests
{
    private Mock<IPaymentRepository> _mockRepo;
    private Mock<IEventProducer<PaymentEventDto>> _mockEventProducer;
    private PaymentOnDeliveryStrategy _strategy;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IPaymentRepository>(MockBehavior.Strict);
        _mockEventProducer = new Mock<IEventProducer<PaymentEventDto>>(MockBehavior.Strict);

        _strategy = new PaymentOnDeliveryStrategy(_mockEventProducer.Object, _mockRepo.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _mockRepo.VerifyNoOtherCalls();
        _mockEventProducer.VerifyNoOtherCalls();
    }

    [Test]
    public async Task InitiatePaymentAsync_ShouldCreatePayment_AndProduceEvent()
    {
        // Arrange
        var order = new Order
        {
            TotalAmount = 99.99m
        };

        var request = new PaymentRequest
        {
            CustomerId = "cust123",
            OrderId = "order456",
            PaymentMethod = PaymentMethod.CashOnDelivery
        };

        Model.Payment? capturedPayment = null;
        PaymentEventDto? capturedEvent = null;

        _mockRepo.Setup(r => r.CreatePaymentAsync(It.IsAny<Model.Payment>()))
                 .Callback<Model.Payment>(p => capturedPayment = p)
                 .Returns(Task.CompletedTask);

        _mockEventProducer.Setup(p => p.ProduceAsync(It.IsAny<PaymentEventDto>(), It.IsAny<CancellationToken>()))
                          .Callback<PaymentEventDto, CancellationToken>((e, _) => capturedEvent = e)
                          .Returns(Task.CompletedTask);

        // Act
        _ = await _strategy.InitiatePaymentAsync(order, request);

        // Assert
        _mockRepo.Verify(r => r.CreatePaymentAsync(It.IsAny<Model.Payment>()), Times.Once);
        Assert.That(capturedPayment, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedPayment.Order, Is.EqualTo(order));
            Assert.That(capturedPayment.Status, Is.EqualTo(nameof(PaymentStatus.PendingOnDelivery)));
        });

        _mockEventProducer.Verify(p => p.ProduceAsync(It.IsAny<PaymentEventDto>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(capturedEvent, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedEvent.CustomerId, Is.EqualTo(request.CustomerId));
            Assert.That(capturedEvent.OrderId, Is.EqualTo(request.OrderId));
            Assert.That(capturedEvent.PaymentId, Is.EqualTo(capturedPayment.Id));
            Assert.That(capturedEvent.Amount, Is.EqualTo(99.99m));
            Assert.That(capturedEvent.EventType, Is.EqualTo(nameof(Events.PaymentPendingOnDelivery)));
        });
    }

    [Test]
    public void CreateRefundAsync_ShouldCompleteWithoutError()
    {
        // Arrange
        var order = new Order();
        var refundRequest = new RefundRequest("","");

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await _strategy.CreateRefundAsync(order, refundRequest));
    }
}
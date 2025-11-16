using EcommerceLib.Exception;
using Moq;
using Payment.Contract;
using Payment.Model;

namespace Payment.Tests;

[TestFixture]
public class AsyncPaymentStrategyTests
{
    private Mock<IPaymentRepository> _mockRepo;
    private Mock<IStripePaymentService> _mockStripeService;
    private AsyncPaymentStrategy _strategy;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IPaymentRepository>(MockBehavior.Strict);
        _mockStripeService = new Mock<IStripePaymentService>(MockBehavior.Strict);

        _strategy = new AsyncPaymentStrategy(_mockRepo.Object, _mockStripeService.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _mockRepo.VerifyNoOtherCalls();
        _mockStripeService.VerifyNoOtherCalls();
    }

    [Test]
    public async Task InitiatePaymentAsync_ShouldCallStripeAndRepository_AndReturnClientSecret()
    {
        // Arrange
        var order = new Order { TotalAmount = 150m };
        var request = new PaymentRequest
        {
            CustomerId = "cust1",
            OrderId = "order1",
            PaymentMethod = PaymentMethod.CreditCard
        };

        var paymentIntentResult = new PaymentIntentResult
        {
            PaymentIntentId = "pi_123",
            ClientSecret = "secret_123"
        };

        _mockStripeService.Setup(s => s.CreatePaymentIntentAsync(order, request))
            .ReturnsAsync(paymentIntentResult);

        Model.Payment? capturedPayment = null;
        _mockRepo.Setup(r => r.CreatePaymentAsync(It.IsAny<Model.Payment>()))
            .Callback<Model.Payment>(p => capturedPayment = p)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _strategy.InitiatePaymentAsync(order, request);

        // Assert
        _mockStripeService.Verify(s => s.CreatePaymentIntentAsync(order, request), Times.Once);
        _mockRepo.Verify(r => r.CreatePaymentAsync(It.IsAny<Model.Payment>()), Times.Once);

        Assert.That(capturedPayment, Is.Not.Null);
        Assert.That(capturedPayment.Id, Is.EqualTo(paymentIntentResult.PaymentIntentId));
        Assert.That(capturedPayment.Status, Is.EqualTo(nameof(PaymentStatus.Pending)));
        Assert.That(capturedPayment.Order, Is.EqualTo(order));

        Assert.That(result.ClientSecret, Is.EqualTo(paymentIntentResult.ClientSecret));
    }

    [Test]
    public async Task CreateRefundAsync_ShouldCallStripe_WhenMappingExists()
    {
        // Arrange
        var order = new Order();
        var refundRequest = new RefundRequest
        (
            CustomerId: "cust1",
            OrderId: "order1"
        );

        _mockRepo.Setup(r => r.GetOrderAsync(refundRequest.CustomerId, refundRequest.OrderId))
            .ReturnsAsync(order);

        _mockStripeService.Setup(s => s.CreateRefundAsync(order, refundRequest))
            .Returns(Task.CompletedTask);

        // Act
        await _strategy.CreateRefundAsync(order, refundRequest);

        // Assert
        _mockStripeService.Verify(s => s.CreateRefundAsync(order, refundRequest), Times.Once);
    }
}
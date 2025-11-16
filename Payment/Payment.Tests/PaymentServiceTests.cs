using EcommerceLib.Contract.Dto;
using EcommerceLib.Exception;
using Moq;
using Payment.Contract;
using Payment.Model;

namespace Payment.Tests;

[TestFixture]
public class PaymentServiceTests
{
    private Mock<IPaymentRepository> _mockRepo;
    private Mock<IPaymentStrategy> _mockStrategy;
    private PaymentService _paymentService;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IPaymentRepository>();
        _mockStrategy = new Mock<IPaymentStrategy>();

        var mockProvider = new MockKeyedServiceProvider(_mockStrategy.Object);
        _paymentService = new PaymentService(_mockRepo.Object, mockProvider);
    }

    [TearDown]
    public void TearDown()
    {
        _mockRepo.VerifyNoOtherCalls();
        _mockStrategy.VerifyNoOtherCalls();
    }

    [Test]
    public void InitiatePayment_ShouldThrow_WhenCustomerOrderMappingIsNull()
    {
        // Arrange
        var request = new PaymentRequest
        {
            CustomerId = "cust123",
            OrderId = "order456",
            PaymentMethod = PaymentMethod.CashOnDelivery
        };

        _mockRepo.Setup(r => r.GetOrderAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Order?)null);

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedException>(
            async () => await _paymentService.InitiatePayment(request));

        _mockRepo.Verify(r => r.GetOrderAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
    
    [Test]
    public async Task InitiatePayment_ShouldResolveCorrectStrategy_AndReturnResult()
    {
        // Arrange
        var order = new Order()
        {
            CustomerId = "cust123",
            OrderId = "order456",
            SelectedPaymentMethod = PaymentMethod.CreditCard,
            TotalAmount = 100.0m
        };
        var expectedResult = new PaymentInitiationResult { ClientSecret = "secret" };

        var request = new PaymentRequest
        {
            CustomerId = "cust123",
            OrderId = "order456",
            PaymentMethod = PaymentMethod.CreditCard
        };

        _mockRepo.Setup(r => r.GetOrderAsync(It.IsAny<string>(), It.IsAny<string>()))
                 .ReturnsAsync(order);

        _mockStrategy.Setup(s => s.InitiatePaymentAsync(order, request))
                     .ReturnsAsync(expectedResult);

        // Act
        var result = await _paymentService.InitiatePayment(request);

        // Assert
        Assert.That(result, Is.EqualTo(expectedResult));
        _mockRepo.Verify(r => r.GetOrderAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockStrategy.Verify(s => s.InitiatePaymentAsync(order, request), Times.Once);
    }
    
    [Test]
    public void CreateOrderAsync_ShouldThrow_WhenOrderIsNull()
    {
        Assert.ThrowsAsync<BadRequestException>(async () =>
            await _paymentService.CreateOrderAsync(null!));
    }

    [Test]
    public async Task CreateOrderAsync_ShouldCallRepository_WhenInputIsValid()
    {
        // Arrange
        var order = new Order() { OrderId = "order456", CustomerId = "cust123", SelectedPaymentMethod = PaymentMethod.CashOnDelivery, TotalAmount = 43.21m };
        
        _mockRepo.Setup(r => r.GetOrderAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(default(Order));

        _mockRepo.Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        // Act
        await _paymentService.CreateOrderAsync(order);

        // Assert
        _mockRepo.Verify(r => r.GetOrderAsync(order.CustomerId, order.OrderId), Times.Once);
        _mockRepo.Verify(r => r.CreateOrderAsync(It.Is<Order>(o 
            => o.OrderId == order.OrderId 
               && o.CustomerId == order.CustomerId 
               && o.SelectedPaymentMethod == order.SelectedPaymentMethod 
               && o.TotalAmount == order.TotalAmount)), Times.Once);
    }
    
    [Test]
    public void UpdatePaymentStatusAsync_ShouldThrow_WhenPaymentNotFound()
    {
        // Arrange
        var paymentId = "nonexistent";

        _mockRepo.Setup(r => r.GetPaymentByIdAsync(paymentId))
            .ReturnsAsync((Model.Payment?)null);

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(async () =>
            await _paymentService.UpdatePaymentStatusAsync(paymentId, PaymentStatus.Paid));

        _mockRepo.Verify(r => r.GetPaymentByIdAsync(paymentId), Times.Once);
    }

    [Test]
    public async Task UpdatePaymentStatusAsync_ShouldUpdateStatus_WhenPaymentExists()
    {
        // Arrange
        var paymentId = "payment123";
        var existingPayment = new Model.Payment
        {
            Id = paymentId,
            Status = nameof(PaymentStatus.Pending)
        };
        Model.Payment? capturedPayment = null;

        _mockRepo.Setup(r => r.GetPaymentByIdAsync(paymentId))
            .ReturnsAsync(existingPayment);

        _mockRepo.Setup(r => r.UpdatePaymentAsync(It.IsAny<Model.Payment>()))
            .Callback<Model.Payment>(p => capturedPayment = p)
            .Returns(Task.CompletedTask);

        // Act
        await _paymentService.UpdatePaymentStatusAsync(paymentId, PaymentStatus.Paid);

        // Assert
        _mockRepo.Verify(r => r.GetPaymentByIdAsync(paymentId), Times.Once);
        _mockRepo.Verify(r => r.UpdatePaymentAsync(It.IsAny<Model.Payment>()), Times.Once);

        Assert.That(capturedPayment, Is.Not.Null);
        Assert.That(capturedPayment.Id, Is.EqualTo(paymentId));
        Assert.That(capturedPayment.Status, Is.EqualTo(nameof(PaymentStatus.Paid)));
    }
    
    [Test]
    public async Task HandleOrderCompletedAsync_ShouldUpdatePaymentStatus()
    {
        // Arrange
        var payment = new Model.Payment
        {
            Id = "payment123",
            Status = nameof(PaymentStatus.Pending)
        };
        var order = new Order()
        {
            CustomerId = "cust123",
            OrderId = "order123",
            SelectedPaymentMethod = PaymentMethod.CashOnDelivery,
            Payment = payment
        };
        var orderDto = new OrderDto
        {
            OrderId = "order123",
            Customer = new CustomerDto { CustomerId = "cust123" }
        };

        _mockRepo.Setup(r => r.GetOrderAsync(orderDto.Customer.CustomerId, orderDto.OrderId))
            .ReturnsAsync(order);

        _mockRepo.Setup(r => r.GetPaymentByIdAsync(payment.Id))
            .ReturnsAsync(payment);

        _mockRepo.Setup(r => r.UpdatePaymentAsync(payment))
            .Returns(Task.CompletedTask);

        // Act
        await _paymentService.HandleOrderCompletedAsync(orderDto);

        // Assert
        _mockRepo.Verify(r => r.GetOrderAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockRepo.Verify(r => r.GetPaymentByIdAsync(payment.Id), Times.Once);
        _mockRepo.Verify(r => r.UpdatePaymentAsync(It.Is<Model.Payment>(p => p.Id == payment.Id && p.Status == nameof(PaymentStatus.Paid))), Times.Once);
    }

    [Test]
    public async Task HandleOrderCancelledAsync_ShouldDoNothing_WhenPaymentIsNotPaid()
    {
        // Arrange
        var payment = new Model.Payment
        {
            Id = "payment123",
            Status = nameof(PaymentStatus.Pending),
        };
        var order = new Order()
        {
            CustomerId = "cust123",
            OrderId = "order123",
            SelectedPaymentMethod = PaymentMethod.CreditCard,
            Payment = payment
        };

        var orderDto = new OrderDto
        {
            OrderId = "order123",
            Customer = new CustomerDto { CustomerId = "cust123" }
        };

        _mockRepo.Setup(r => r.GetOrderAsync(orderDto.Customer.CustomerId, orderDto.OrderId))
                 .ReturnsAsync(order);

        // Act
        await _paymentService.HandleOrderCancelledAsync(orderDto);

        // Assert
        _mockRepo.Verify(r => r.GetOrderAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockStrategy.Verify(s => s.CreateRefundAsync(It.IsAny<Order>(), It.IsAny<RefundRequest>()), Times.Never);
    }

    [Test]
    public async Task HandleOrderCancelledAsync_ShouldCallCreateRefund_WhenPaymentIsPaid()
    {
        // Arrange
        var payment = new Model.Payment
        {
            Id = "payment123",
            Status = nameof(PaymentStatus.Paid),
        };
        var order = new Order()
        {
            CustomerId = "cust123",
            OrderId = "order123",
            SelectedPaymentMethod = PaymentMethod.CreditCard,
            Payment = payment
        };

        var orderDto = new OrderDto
        {
            OrderId = "order123",
            Customer = new CustomerDto { CustomerId = "cust123" }
        };

        _mockRepo.Setup(r => r.GetOrderAsync(It.IsAny<string>(), It.IsAny<string>()))
                 .ReturnsAsync(order);

        _mockStrategy.Setup(s => s.CreateRefundAsync(order, It.IsAny<RefundRequest>()))
                     .Returns(Task.CompletedTask)
                     .Verifiable();

        // Act
        await _paymentService.HandleOrderCancelledAsync(orderDto);

        // Assert
        _mockRepo.Verify(r => r.GetOrderAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockStrategy.Verify(s => s.CreateRefundAsync(order, It.Is<RefundRequest>(r =>
            r.CustomerId == orderDto.Customer.CustomerId && r.OrderId == orderDto.OrderId)), Times.Once);
    }
    
    
}
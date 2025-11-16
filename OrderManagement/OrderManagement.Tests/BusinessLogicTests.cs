using System.Net;
using EcommerceLib;
using EcommerceLib.Auth;
using EcommerceLib.Contract;
using EcommerceLib.Contract.Dto;
using EcommerceLib.Contract.Events;
using EcommerceLib.Exception;
using EcommerceLib.Messaging;
using Moq;
using OrderManagement.Common.Data;
using OrderManagement.Contract;
using OrderManagement.Inventory;
using OrderManagement.Payment;
using OrderManagement.Shipping;
using OrderManagement.Shipping.Contract;

namespace OrderManagement.Tests;

public class BusinessLogicTests
{
    private Mock<IShippingClient> _shippingClientMock;
    private Mock<IPaymentClient> _paymentClientMock;
    private Mock<IInventoryClient> _inventoryClientMock;
    private Mock<IOrderRepository> _repositoryMock;
    private Mock<IEventProducer<OrderEventDto>> _eventProducerMock;
    private OrderManagementService _service;
    private const string CustomerId = "customer1";

    [SetUp]
    public void SetUp()
    {
        _shippingClientMock = new Mock<IShippingClient>();
        _paymentClientMock = new Mock<IPaymentClient>();
        _inventoryClientMock = new Mock<IInventoryClient>();
        _repositoryMock = new Mock<IOrderRepository>();
        _eventProducerMock = new Mock<IEventProducer<OrderEventDto>>();

        _service = new OrderManagementService(
            _inventoryClientMock.Object,
            _shippingClientMock.Object,
            _paymentClientMock.Object,
            _repositoryMock.Object,
            _eventProducerMock.Object
        );
    }

    [Test]
    public async Task CreateOrderAsync_ShouldCreateOrder_WhenAllServicesSucceed()
    {
        // Arrange
        var cartEvent = CreateValidCartEvent();

        _shippingClientMock.Setup(c => c.GetShippingFeeAsync(cartEvent.ShippingMethod))
            .ReturnsAsync(new ShippingFeeResult()
            {
                StatusCode = HttpStatusCode.OK,
                Fee = new ShippingFeeDto(
                    nameof(ShippingMethod.Express),
                    5,
                    DateTime.UtcNow.AddDays(3).ToShortDateString()
                )
            });
        _repositoryMock.Setup(r => r.CreateOrderAsync(It.IsAny<Order>())).ReturnsAsync("");  

        _paymentClientMock.Setup(c => c.GetPaymentFeeAsync(cartEvent.PaymentMethod))
            .ReturnsAsync(new PaymentFeeResult()
            {
                StatusCode = HttpStatusCode.OK,
                Fee = new PaymentFeeDto
                (
                    cartEvent.PaymentMethod,
                    2
                )
            });

        _inventoryClientMock.Setup(c => c.TryReserveStockAsync(It.IsAny<StockReservationRequest>()))
            .ReturnsAsync(new HttpResponseMessage() { StatusCode = HttpStatusCode.OK });

        // Act
        await _service.CreateOrderAsync(cartEvent);

        // Assert
        _repositoryMock.Verify(r => r.CreateOrderAsync(It.IsAny<Order>()), Times.Once);
    }
    
    [Test]
    public async Task CreateOrderAsync_ShouldFail_WhenShippingRateFails()
    {
        // Arrange
        var cartEvent = CreateValidCartEvent();

        _shippingClientMock.Setup(c => c.GetShippingFeeAsync(cartEvent.ShippingMethod))
            .ReturnsAsync(new ShippingFeeResult
            {
                StatusCode = HttpStatusCode.BadRequest,
                Fee = null
            });

        // Act
        await _service.CreateOrderAsync(cartEvent);

        // Assert
        _repositoryMock.Verify(r => r.CreateOrderAsync(It.IsAny<Order>()), Times.Never);
        _eventProducerMock.Verify(e => e.ProduceAsync(It.Is<OrderEventDto>(dto => dto.EventType == nameof(Events.OrderCreationFailed)), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Test]
    public async Task CreateOrderAsync_ShouldFail_WhenPaymentRateFails()
    {
        // Arrange
        var cartEvent = CreateValidCartEvent();

        _shippingClientMock.Setup(c => c.GetShippingFeeAsync(cartEvent.ShippingMethod))
            .ReturnsAsync(new ShippingFeeResult
            {
                StatusCode = HttpStatusCode.OK,
                Fee = new ShippingFeeDto(
                    nameof(ShippingMethod.Express),
                    5,
                    DateTime.UtcNow.AddDays(3).ToShortDateString()
                )
            });

        _paymentClientMock.Setup(c => c.GetPaymentFeeAsync(cartEvent.PaymentMethod))
            .ReturnsAsync(new PaymentFeeResult
            {
                StatusCode = HttpStatusCode.BadRequest,
                Fee = null
            });

        // Act
        await _service.CreateOrderAsync(cartEvent);

        // Assert
        _repositoryMock.Verify(r => r.CreateOrderAsync(It.IsAny<Order>()), Times.Never);
        _eventProducerMock.Verify(e => e.ProduceAsync(It.Is<OrderEventDto>(dto => dto.EventType == nameof(Events.OrderCreationFailed)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateOrderAsync_ShouldFail_WhenStockReservationFails()
    {
        // Arrange
        var cartEvent = CreateValidCartEvent();

        _shippingClientMock.Setup(c => c.GetShippingFeeAsync(cartEvent.ShippingMethod))
            .ReturnsAsync(new ShippingFeeResult
            {
                StatusCode = HttpStatusCode.OK,
                Fee = new ShippingFeeDto(
                    nameof(ShippingMethod.Express),
                    5,
                    DateTime.UtcNow.AddDays(3).ToShortDateString()
                )
            });

        _paymentClientMock.Setup(c => c.GetPaymentFeeAsync(cartEvent.PaymentMethod))
            .ReturnsAsync(new PaymentFeeResult
            {
                StatusCode = HttpStatusCode.OK,
                Fee = new PaymentFeeDto(cartEvent.PaymentMethod, 2)
            });

        _inventoryClientMock.Setup(c => c.TryReserveStockAsync(It.IsAny<StockReservationRequest>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest });

        // Act
        await _service.CreateOrderAsync(cartEvent);

        // Assert
        _repositoryMock.Verify(r => r.CreateOrderAsync(It.IsAny<Order>()), Times.Never);
        _eventProducerMock.Verify(e => e.ProduceAsync(It.Is<OrderEventDto>(dto => dto.EventType == nameof(Events.OrderCreationFailed)), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Test]
    public async Task UpdateStatusAsync_ShouldAddNewStatus_WhenOrderExists()
    {
        // Arrange
        var orderId = "order-123";
        var order = new Order
        {
            Id = orderId,
            StatusHistory = [new OrderStatus { OrderId = orderId, Status = nameof(Status.Created) }]
        };
        _repositoryMock.Setup(r => r.UpdateOrderAsync(order)).Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        await _service.UpdateStatusAsync(orderId, Status.Paid);

        // Assert
        Assert.That(order.StatusHistory.Count, Is.EqualTo(2));
        Assert.That(order.StatusHistory.Last().Status, Is.EqualTo(nameof(Status.Paid)));
        _repositoryMock.Verify(r => r.UpdateOrderAsync(order), Times.Once);
    }

    [Test]
    public void UpdateStatusAsync_ShouldThrowNotFoundException_WhenOrderDoesNotExist()
    {
        // Arrange
        var orderId = "nonexistent";
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateStatusAsync(orderId, Status.Cancelled));
        _repositoryMock.Verify(r => r.UpdateOrderAsync(It.IsAny<Order>()), Times.Never);
    }

    [Test]
    public async Task UpdateStatusAsync_ShouldAppendMultipleStatuses_WhenCalledMultipleTimes()
    {
        // Arrange
        var orderId = "order-456";
        var order = CreateOrder("customer1", Status.Pending);
        order.Id = orderId;

        _repositoryMock.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);
        _repositoryMock.Setup(r => r.UpdateOrderAsync(order)).Returns(Task.CompletedTask);

        // Act && Assert
        await _service.UpdateStatusAsync(orderId, Status.Created);
        Assert.That(order.CurrentStatus, Is.EqualTo(nameof(Status.Created)));
        
        await _service.UpdateStatusAsync(orderId, Status.Completed);
        Assert.That(order.CurrentStatus, Is.EqualTo(nameof(Status.Completed)));
        Assert.That(order.StatusHistory, Has.Count.EqualTo(3));
        _repositoryMock.Verify(r => r.UpdateOrderAsync(order), Times.Exactly(2));
    }
    
    [Test]
    public async Task FulfillOrderAsync_ShouldFulfillOrder_WhenOrderExistsAndIsPending()
    {
        // Arrange
        var orderId = "order-123";
        var order = CreateOrder(CustomerId, Status.Pending);
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(order);
        _repositoryMock.Setup(r => r.UpdateOrderAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);

        // Act
        await _service.FulfillOrderAsync(orderId);

        // Assert
        Assert.That(order.StatusHistory, Has.Count.EqualTo(2));
        Assert.That(order.StatusHistory.Last().Status, Is.EqualTo(nameof(Status.Created)));
        _repositoryMock.Verify(r => r.UpdateOrderAsync(It.IsAny<Order>()), Times.Once);
        _eventProducerMock.Verify(e => e.ProduceAsync(It.IsAny<OrderEventDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void FulfillOrderAsync_ShouldThrowNotFoundException_WhenOrderDoesNotExist()
    {
        // Arrange
        var orderId = "does-not-exist";
        _repositoryMock.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(() => _service.FulfillOrderAsync(orderId));

        _repositoryMock.Verify(r => r.UpdateOrderAsync(It.IsAny<Order>()), Times.Never);
        _eventProducerMock.Verify(e => e.ProduceAsync(It.IsAny<OrderEventDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void FulfillOrderAsync_ShouldThrowBadRequestException_WhenOrderIsNotPending()
    {
        // Arrange
        var orderId = "order-456";
        var order = new Order
        {
            Id = orderId,
            StatusHistory = new List<OrderStatus>
            {
                new OrderStatus { OrderId = orderId, Status = nameof(Status.Completed) }
            }
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act & Assert
        Assert.ThrowsAsync<BadRequestException>(() => _service.FulfillOrderAsync(orderId));

        _repositoryMock.Verify(r => r.UpdateOrderAsync(It.IsAny<Order>()), Times.Never);
        _eventProducerMock.Verify(e => e.ProduceAsync(It.IsAny<OrderEventDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CancelOrderAsync_ShouldCancelOrder_WhenUserIsOwner()
    {
        // Arrange
        var order = CreateOrder(Customer.Id, Status.Pending);
        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _repositoryMock.Setup(r => r.UpdateOrderAsync(order)).Returns(Task.CompletedTask);

        // Act
        await _service.CancelOrderAsync(Customer, order.Id, "Changed my mind");

        // Assert
        Assert.That(order.StatusHistory, Has.Count.EqualTo(2));
        Assert.That(order.StatusHistory[^1].Status, Is.EqualTo(nameof(Status.Cancelled)));
        _repositoryMock.Verify(r => r.UpdateOrderAsync(order), Times.Once);
        _eventProducerMock.Verify(e => e.ProduceAsync(It.IsAny<OrderEventDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CancelOrderAsync_ShouldCancelOrder_WhenUserIsAdmin()
    {
        // Arrange
        var order = CreateOrder( "other-customer", Status.Pending);
        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _repositoryMock.Setup(r => r.UpdateOrderAsync(order)).Returns(Task.CompletedTask);

        // Act
        await _service.CancelOrderAsync(AdminUser, order.Id, "Admin cancelled");

        // Assert
        Assert.That(order.StatusHistory[^1].Status, Is.EqualTo(nameof(Status.Cancelled)));
        _repositoryMock.Verify(r => r.UpdateOrderAsync(order), Times.Once);
        _eventProducerMock.Verify(e => e.ProduceAsync(It.IsAny<OrderEventDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void CancelOrderAsync_ShouldThrowNotFoundException_WhenOrderDoesNotExist()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync("nonexistent")).ReturnsAsync((Order?)null);

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(() => _service.CancelOrderAsync(Customer, "nonexistent", null));
        _repositoryMock.Verify(r => r.UpdateOrderAsync(It.IsAny<Order>()), Times.Never);
        _eventProducerMock.Verify(e => e.ProduceAsync(It.IsAny<OrderEventDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void CancelOrderAsync_ShouldThrowNotFoundException_WhenUserIsNotOwnerOrAdmin()
    {
        // Arrange
        var order = CreateOrder( "other-customer", Status.Pending);
        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(() => _service.CancelOrderAsync(Customer, order.Id, null));
        _repositoryMock.Verify(r => r.UpdateOrderAsync(It.IsAny<Order>()), Times.Never);
        _eventProducerMock.Verify(e => e.ProduceAsync(It.IsAny<OrderEventDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase(Status.Cancelled)]
    [TestCase(Status.Completed)]
    public void CancelOrderAsync_ShouldThrowBadRequestException_WhenOrderAlreadyCancelledOrCompleted(Status status)
    {
        // Arrange
        var order = CreateOrder(Customer.Id, status);
        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        // Act & Assert
        Assert.ThrowsAsync<BadRequestException>(() => _service.CancelOrderAsync(Customer, order.Id, null));
        _repositoryMock.Verify(r => r.UpdateOrderAsync(It.IsAny<Order>()), Times.Never);
        _eventProducerMock.Verify(e => e.ProduceAsync(It.IsAny<OrderEventDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Test]
    public async Task GetOrderHistoryAsync_ShouldReturnOrdersForCustomer()
    {
        // Arrange
        var orders = new List<Order>
        {
            new() { Id = "order-1", CustomerDetails = new CustomerDetails { CustomerId = Customer.Id } },
            new() { Id = "order-2", CustomerDetails = new CustomerDetails { CustomerId = Customer.Id } }
        };

        _repositoryMock.Setup(r => r.GetOrderHistoryAsync(Customer.Id))
            .ReturnsAsync(orders);

        // Act
        var result = await _service.GetOrderHistoryAsync(Customer);

        // Assert
        _repositoryMock.Verify(r => r.GetOrderHistoryAsync(Customer.Id), Times.Once);
        var enumerable = result.ToList();
        Assert.That(enumerable, Is.Not.Null);
        Assert.That(enumerable, Has.Count.EqualTo(2));
        Assert.That(enumerable.First().CustomerDetails.CustomerId, Is.EqualTo(Customer.Id));
    }
    
    [Test]
    public async Task GetByIdAsync_ShouldReturnOrder_WhenOrderExists()
    {
        // Arrange
        var orderId = "order-123";
        var checkoutId = "checkout-456";

        var order = new Order
        {
            Id = orderId,
            CustomerDetails = new CustomerDetails { CustomerId = Customer.Id }
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(Customer.Id, checkoutId, orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _service.GetByIdAsync(Customer.Id, checkoutId, orderId);

        // Assert
        _repositoryMock.Verify(r => r.GetByIdAsync(Customer.Id, checkoutId, orderId), Times.Once);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(orderId));
        Assert.That(result.CustomerDetails.CustomerId, Is.EqualTo(Customer.Id));
    }

    [Test]
    public void GetByIdAsync_ShouldThrowNotFoundException_WhenOrderDoesNotExist()
    {
        // Arrange
        var orderId = "nonexistent";
        var checkoutId = "checkout-456";

        _repositoryMock.Setup(r => r.GetByIdAsync(Customer.Id, checkoutId, orderId))
            .ReturnsAsync((Order?)null);

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(Customer.Id, checkoutId, orderId));
        _repositoryMock.Verify(r => r.GetByIdAsync(Customer.Id, checkoutId, orderId), Times.Once);
    }
    
    private static AppUser Customer => new AppUser
    {
        Id = CustomerId,
        Roles = new List<string> { nameof(Roles.Customer) }
    };

    private static AppUser AdminUser => new AppUser
    {
        Id = CustomerId,
        Roles = new List<string> { nameof(Roles.Admin) }
    };
    
    private CartCheckedOutEventDto CreateValidCartEvent()
    {
        return new CartCheckedOutEventDto
        {
            CheckoutId = Guid.NewGuid().ToString(),
            CustomerId = CustomerId,
            PaymentMethod = "CreditCard",
            ShippingMethod = "Express",
            CustomerDetails = new EcommerceLib.Contract.Dto.CustomerDetailsDto("John Doe", "john@example.com", "555-1234"),
            ShippingAddress = new AddressDto
            {
                AddressLine = "123 Main St",
                City = "Springfield",
                State = "IL",
                PostalCode = "62704",
                Country = "USA"
            },
            BillingAddress = new AddressDto
            {
                AddressLine = "456 Oak Ave",
                City = "Springfield",
                State = "IL",
                PostalCode = "62705",
                Country = "USA"
            },
            Items = [new CartItemDto("prod1", 2, 10)],
            CustomerNotes = "Leave at the porch"
        };
    }
    
    private static Order CreateOrder(string customerId, Status status) => new Order
    {
        Id = "order-123",
        CustomerDetails = new CustomerDetails
        {
            CustomerId = customerId,
            FullName = "John Doe",
            Email = "john@example.com",
            PhoneNumber = "555-1234"
        },
        ShippingAddress = new Address
        {
            AddressLine = "123 Main St",
            City = "Springfield",
            State = "IL",
            PostalCode = "62704",
            Country = "USA"
        },
        BillingAddress = new Address
        {
            AddressLine = "456 Oak Ave",
            City = "Springfield",
            State = "IL",
            PostalCode = "62705",
            Country = "USA"
        },
        StatusHistory = new List<OrderStatus>
        {
            new() { OrderId = "order-123", Status = status.ToString() }
        },
        OrderItems = new List<OrderItem>
        {
            new() { ProductId = "prod1", Quantity = 2, UnitPrice = 10 }
        },
        ShippingMethod = "Express",
        PaymentMethod = "CreditCard",
        CreatedAt = DateTime.UtcNow,
    };
}
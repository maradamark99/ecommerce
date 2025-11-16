using System.Net;
using System.Net.Http.Json;
using EcommerceLib.Contract.Dto;
using EcommerceLib.Contract.Events;
using EcommerceLib.Testing.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Common.Data;
using OrderManagement.Inventory;
using OrderManagement.Payment;
using OrderManagement.Shipping;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using CustomerDetailsDto = EcommerceLib.Contract.Dto.CustomerDetailsDto;

namespace OrderManagement.Tests;

public class ServiceLevelTests
{
    private TestEnvironment<ITestEnvironmentMarker> _testEnvironment;
    private const string CustomerId = "customer1";
    private const string OrderManagement = "api/v1/order-management";
    private const string Inventory = "/api/v1/inventory";
    private const string Payments = "/api/v1/payments";
    private const string Shipping = "/api/v1/shipping";
    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _testEnvironment = TestEnvironment<ITestEnvironmentMarker>.Builder
            .WithDatabase<AppDbContext>()
            .WithMockServer()
            .WithProducer()
            .WithConsumer()
            .WithServiceOverride(c =>
            {
                c.Configure<ShippingClientConfig>(config =>
                {
                    config.BaseUrl = _testEnvironment.MockServer!.Url!;
                });
                c.Configure<InventoryClientConfig>(config =>
                {
                    config.BaseUrl = _testEnvironment.MockServer!.Url!;
                });
                c.Configure<PaymentClientConfig>(config =>
                {
                    config.BaseUrl = _testEnvironment.MockServer!.Url!;
                }); 
            })
            .Build();
        await _testEnvironment.StartAsync();
    }
    
    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _testEnvironment.DisposeAsync();
    }
    
    [SetUp]
    public async Task SetUp()
    {
        await _testEnvironment.ResetAsync();
    }

    [Test]
    public async Task GetById_OrderIdAndCheckoutIdNotGiven_ReturnsBadRequest()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();

        // Act
        var response = await client.GetAsync(OrderManagement);
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetById_OrderDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        
        // Act
        var response = await client.GetAsync(OrderManagement + "?orderId=order123");
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
    
    [Test]
    public async Task GetById_OrderExists_ReturnsOrder()
    {
        // Arrange
        const string orderId = "order123";
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var expectedOrder = CreateTestOrder(orderId, CustomerId);
        await db.Orders.AddAsync(expectedOrder);
        await db.SaveChangesAsync();

        // Act
        var response = await client.GetAsync(OrderManagement + $"?orderId={orderId}");
        var order = await response.Content.ReadFromJsonAsync<Order>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            Assert.That(order, Is.Not.Null);
            Assert.That(order!.Id, Is.EqualTo(expectedOrder.Id));
            Assert.That(order.CustomerDetails.CustomerId, Is.EqualTo(expectedOrder.CustomerDetails.CustomerId));
            Assert.That(order.CheckoutId, Is.EqualTo(expectedOrder.CheckoutId));
            
            Assert.That(order.OrderItems, Has.Count.EqualTo(expectedOrder.OrderItems.Count));
            for (var i = 0; i < expectedOrder.OrderItems.Count; i++)
            {
                Assert.That(order.OrderItems[i].ProductId, Is.EqualTo(expectedOrder.OrderItems[i].ProductId));
                Assert.That(order.OrderItems[i].Quantity, Is.EqualTo(expectedOrder.OrderItems[i].Quantity));
                Assert.That(order.OrderItems[i].UnitPrice, Is.EqualTo(expectedOrder.OrderItems[i].UnitPrice));
            }

            Assert.That(order.StatusHistory.Last().Status, Is.EqualTo(nameof(Status.Pending)));

            Assert.That(order.ShippingAddress.City, Is.EqualTo(expectedOrder.ShippingAddress.City));
            Assert.That(order.BillingAddress.City, Is.EqualTo(expectedOrder.BillingAddress.City));

            Assert.That(order.ItemTotal, Is.EqualTo(expectedOrder.ItemTotal));
            Assert.That(order.Total, Is.EqualTo(expectedOrder.Total));

            Assert.That(order.PaymentMethod, Is.EqualTo(expectedOrder.PaymentMethod));
            Assert.That(order.ShippingMethod, Is.EqualTo(expectedOrder.ShippingMethod));
            Assert.That(order.CustomerNotes, Does.Contain(expectedOrder.CustomerNotes));
            Assert.That(order.EstimatedDeliveryDate?.Date, Is.EqualTo(expectedOrder.EstimatedDeliveryDate?.Date));
        });
    }

    [Test]
    public async Task CreateOrder_ShippingCallFails_ShouldProduceOrderCreationFailedEvent()
    {
        // Arrange
        var checkoutId = Guid.NewGuid().ToString();
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        
        _testEnvironment.MockServer!
            .Given(Request.Create().WithPath(Shipping + "/*")
                .UsingGet())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.BadRequest));
        
        // Act
        await _testEnvironment.EventProducer!.ProduceAsync(Topics.CartEvents, CreateValidCartEvent(checkoutId), Guid.NewGuid().ToString());
        await Task.Delay(TimeSpan.FromSeconds(1));
        
        // Assert
        _testEnvironment.EventConsumer!.ConsumeWithTimeout<OrderEventDto>(Topics.OrderEvents, TimeSpan.FromSeconds(1));
        Assert.That(_testEnvironment.EventConsumer!.HasMessageContaining<OrderEventDto>(o => o.EventType == nameof(Events.OrderCreationFailed) && o.Order.OrderId == checkoutId), Is.True);
    }
    
    [Test]
    public async Task CreateOrder_PaymentCallFails_ShouldProduceOrderCreationFailedEvent()
    {
        // Arrange
        var checkoutId = Guid.NewGuid().ToString();
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();

        _testEnvironment.MockServer!
            .Given(Request.Create().WithPath(Shipping + "/*").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBodyAsJson(new { 
                    ShippingMethod = "Standard",
                    Fee = 10.0m,
                    EstimatedShippingDate = DateTime.UtcNow.AddDays(3).ToLongDateString() 
                }));

        _testEnvironment.MockServer!
            .Given(Request.Create().WithPath(Payments + "/*").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.BadRequest));

        await _testEnvironment.EventProducer!.ProduceAsync(
            Topics.CartEvents,
            CreateValidCartEvent(checkoutId),
            Guid.NewGuid().ToString());

        await Task.Delay(TimeSpan.FromSeconds(1));

        // Act + Assert
        _testEnvironment.EventConsumer!.ConsumeWithTimeout<OrderEventDto>(
            Topics.OrderEvents, TimeSpan.FromSeconds(1));

        Assert.That(_testEnvironment.EventConsumer!.HasMessageContaining<OrderEventDto>(o =>
            o.EventType == nameof(Events.OrderCreationFailed)
            && o.Order.OrderId == checkoutId), Is.True);
    }
    
    [Test]
    public async Task CreateOrder_InventoryCallFails_ShouldProduceOrderCreationFailedEvent()
    {
        // Arrange
        var checkoutId = Guid.NewGuid().ToString();
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();

        _testEnvironment.MockServer!
            .Given(Request.Create().WithPath(Shipping + "/*").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBodyAsJson(new { 
                    ShippingMethod = "Standard",
                    Fee = 10.0m,
                    EstimatedShippingDate = DateTime.UtcNow.AddDays(3).ToLongDateString() 
                }));

        _testEnvironment.MockServer!
            .Given(Request.Create().WithPath(Payments + "/*").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBodyAsJson(new { PaymentMethod = "CreditCard", Fee = 2.5m }));

        _testEnvironment.MockServer!
            .Given(Request.Create().WithPath(Inventory + "/reserve-stock").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.BadRequest));

        // Act 
        await _testEnvironment.EventProducer!.ProduceAsync(
            Topics.CartEvents,
            CreateValidCartEvent(checkoutId),
            Guid.NewGuid().ToString());
        await Task.Delay(TimeSpan.FromSeconds(1));

        // Assert
        _testEnvironment.EventConsumer!.ConsumeWithTimeout<OrderEventDto>(
            Topics.OrderEvents, TimeSpan.FromSeconds(1));

        Assert.That(_testEnvironment.EventConsumer!.HasMessageContaining<OrderEventDto>(o =>
            o.EventType == nameof(Events.OrderCreationFailed)
            && o.Order.OrderId == checkoutId), Is.True);
    }
    
    [Test]
    public async Task CreateOrder_AllExternalCallsSucceed_ShouldCreateOrderInDatabase()
    {
        // Arrange
        var checkoutId = Guid.NewGuid().ToString();
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();

        _testEnvironment.MockServer!
            .Given(Request.Create().WithPath(Shipping + "/*").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBodyAsJson(new {
                    ShippingMethod = "Standard",
                    Fee = 10.0m,
                    EstimatedShippingDate = DateTime.UtcNow.AddDays(3).ToLongDateString() 
                    }
                )
            );

        _testEnvironment.MockServer!
            .Given(Request.Create().WithPath(Payments + "/*").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBodyAsJson(new { PaymentMethod = "CreditCard", Fee = 2.5m }));

        _testEnvironment.MockServer!
            .Given(Request.Create().WithPath(Inventory + "/reserve-stock").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK));

        // Act
        await _testEnvironment.EventProducer!.ProduceAsync(
            Topics.CartEvents,
            CreateValidCartEvent(checkoutId),
            Guid.NewGuid().ToString());
        await Task.Delay(TimeSpan.FromSeconds(1));
        
        // Assert
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var createdOrder = await db.Orders
            .AsNoTracking()
            .Include(o => o.CustomerDetails)
            .Include(o => o.OrderItems)
            .Include(o => o.StatusHistory)
            .Include(o => o.ShippingAddress)
            .Include(o => o.BillingAddress)
            .FirstOrDefaultAsync(o => o.CheckoutId == checkoutId);

        Assert.That(createdOrder, Is.Not.Null);   
        Assert.Multiple(() =>
        {
            Assert.That(createdOrder, Is.Not.Null, "Order should be persisted in the DB");
            Assert.That(createdOrder.CheckoutId, Is.EqualTo(checkoutId));
            Assert.That(createdOrder.CustomerDetails, Is.Not.Null);
            Assert.That(createdOrder.OrderItems, Is.Not.Empty);
            Assert.That(createdOrder.StatusHistory.Last().Status, Is.EqualTo(nameof(Status.Pending)));
            Assert.That(createdOrder.Total, Is.GreaterThan(0));
            Assert.That(createdOrder.PaymentMethod, Is.EqualTo("CreditCard"));
            Assert.That(createdOrder.ShippingMethod, Is.EqualTo("Standard"));
        });
        
        _testEnvironment.EventConsumer!.ConsumeWithTimeout<OrderEventDto>(
            Topics.OrderEvents, TimeSpan.FromSeconds(1));

        Assert.That(_testEnvironment.EventConsumer!.HasMessageContaining<OrderEventDto>(o =>
            o.EventType == nameof(Events.OrderCreated)
            && o.Order.OrderId == createdOrder.Id), Is.True);
    }
    
    [Test]
    public async Task GetOrderHistory_OrdersExist_ReturnsOrdersForCustomer()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order1 = CreateTestOrder("order-history-1", CustomerId);
        var order2 = CreateTestOrder("order-history-2", CustomerId);
        var otherCustomerOrder = CreateTestOrder("order-history-other", "other-customer");

        await db.Orders.AddRangeAsync(order1, order2, otherCustomerOrder);
        await db.SaveChangesAsync();

        // Act
        var response = await client.GetAsync(OrderManagement + "/history");
        var orders = await response.Content.ReadFromJsonAsync<List<Order>>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(orders, Is.Not.Null);
        Assert.That(orders, Has.Count.EqualTo(2), "Should return only the current customer's orders");
        Assert.That(orders!.Select(o => o.Id), Does.Contain(order1.Id));
        Assert.That(orders.Select(o => o.Id), Does.Contain(order2.Id));
        Assert.That(orders.Select(o => o.Id), Does.Not.Contain(otherCustomerOrder.Id));
    }
    
    [Test]
    public async Task ShippingEvent_ShipmentDelivered_ShouldMarkOrderAsCompleted()
    {
        // Arrange
        const string orderId = "order-shipment-1";

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = CreateTestOrder(orderId, CustomerId);
        await db.Orders.AddAsync(order);
        await db.SaveChangesAsync();

        var shippingEvent = new ShippingEventDto
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.ShipmentDelivered),
            OrderId = orderId,
            Customer = new CustomerDto()
            {
                CustomerId = order.CustomerDetails.CustomerId,
                FullName = order.CustomerDetails.FullName,
            }
        };

        // Act
        await _testEnvironment.EventProducer!.ProduceAsync(
            Topics.ShippingEvents,
            shippingEvent,
            Guid.NewGuid().ToString());
        await Task.Delay(TimeSpan.FromSeconds(1));

        // Assert
        var updatedOrder = await db.Orders
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        Assert.That(updatedOrder, Is.Not.Null);
        Assert.That(updatedOrder.StatusHistory.Last().Status, Is.EqualTo(nameof(Status.Completed)));
        
        _testEnvironment.EventConsumer!.ConsumeWithTimeout<OrderEventDto>(Topics.OrderEvents, TimeSpan.FromSeconds(1));
        Assert.That(_testEnvironment.EventConsumer!.HasMessageContaining<OrderEventDto>(o =>
            o is { EventType: nameof(Events.OrderCompleted), Order.OrderId: orderId }), Is.True);
    }
    
    [Test]
    public async Task PaymentEvent_PaymentSucceeded_ShouldFulfillOrder()
    {
        // Arrange
        const string orderId = "order-payment-1";

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = CreateTestOrder(orderId, CustomerId);
        order.StatusHistory.Clear();
        order.StatusHistory.Add(new OrderStatus { OrderId = orderId, Status = nameof(Status.Pending) });

        await db.Orders.AddAsync(order);
        await db.SaveChangesAsync();

        var paymentEvent = new PaymentEventDto
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.PaymentSucceeded),
            OrderId = orderId,
            CustomerId = order.CustomerDetails.CustomerId
        };

        // Act
        await _testEnvironment.EventProducer!.ProduceAsync(
            Topics.PaymentEvents,
            paymentEvent,
            Guid.NewGuid().ToString());

        await Task.Delay(TimeSpan.FromSeconds(1));

        // Assert
        var updatedOrder = await db.Orders
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        Assert.That(updatedOrder, Is.Not.Null);
        Assert.That(updatedOrder.StatusHistory.Last().Status, Is.EqualTo(nameof(Status.Created)));

        _testEnvironment.EventConsumer!.ConsumeWithTimeout<OrderEventDto>(Topics.OrderEvents, TimeSpan.FromSeconds(1));
        Assert.That(_testEnvironment.EventConsumer!.HasMessageContaining<OrderEventDto>(o =>
            o is { EventType: nameof(Events.FulfillOrder), Order.OrderId: orderId }), Is.True);
    }
    
    [Test]
    public async Task PaymentEvent_PaymentFailed_ShouldCancelOrder()
    {
        // Arrange
        const string orderId = "order-cancel-payment";

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = CreateTestOrder(orderId, CustomerId);
        order.StatusHistory.Clear();
        order.StatusHistory.Add(new OrderStatus { OrderId = orderId, Status = nameof(Status.Pending) });

        await db.Orders.AddAsync(order);
        await db.SaveChangesAsync();

        var paymentEvent = new PaymentEventDto
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.PaymentFailed),
            OrderId = orderId,
            CustomerId = order.CustomerDetails.CustomerId
        };

        // Act
        await _testEnvironment.EventProducer!.ProduceAsync(
            Topics.PaymentEvents,
            paymentEvent,
            Guid.NewGuid().ToString());

        await Task.Delay(TimeSpan.FromSeconds(1)); 

        // Assert
        var updatedOrder = await db.Orders.Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        Assert.That(updatedOrder, Is.Not.Null);
        Assert.That(updatedOrder.StatusHistory.Last().Status, Is.EqualTo(nameof(Status.Cancelled)));

        _testEnvironment.EventConsumer!.ConsumeWithTimeout<OrderEventDto>(Topics.OrderEvents, TimeSpan.FromSeconds(1));
        Assert.That(_testEnvironment.EventConsumer!.HasMessageContaining<OrderEventDto>(o =>
            o is { EventType: nameof(Events.OrderCancelled), Order.OrderId: orderId }), Is.True);
    }
    
    [Test]
    public async Task ShippingEvent_ShipmentFailed_ShouldCancelOrder()
    {
        // Arrange
        const string orderId = "order-cancel-shipping";

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = CreateTestOrder(orderId, CustomerId);
        order.StatusHistory.Clear();
        order.StatusHistory.Add(new OrderStatus { OrderId = orderId, Status = nameof(Status.Pending) });

        await db.Orders.AddAsync(order);
        await db.SaveChangesAsync();

        var shippingEvent = new ShippingEventDto
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.ShipmentFailed),
            OrderId = orderId,
            Customer = new CustomerDto
            {
                CustomerId = order.CustomerDetails.CustomerId,
                FullName = order.CustomerDetails.FullName,
            }
        };

        await _testEnvironment.EventProducer!.ProduceAsync(
            Topics.ShippingEvents,
            shippingEvent,
            Guid.NewGuid().ToString());

        await Task.Delay(TimeSpan.FromSeconds(1)); 

        // Assert
        var updatedOrder = await db.Orders.Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        Assert.That(updatedOrder, Is.Not.Null);
        Assert.That(updatedOrder.StatusHistory.Last().Status, Is.EqualTo(nameof(Status.Cancelled)));

        _testEnvironment.EventConsumer!.ConsumeWithTimeout<OrderEventDto>(Topics.OrderEvents, TimeSpan.FromSeconds(1));
        Assert.That(_testEnvironment.EventConsumer!.HasMessageContaining<OrderEventDto>(o =>
            o is { EventType: nameof(Events.OrderCancelled), Order.OrderId: orderId }), Is.True);
    }
    
    [Test]
    public async Task CancelOrder_Api_ShouldCancelOrderForCustomer()
    {
        // Arrange
        const string orderId = "order-cancel-api";
      
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = CreateTestOrder(orderId, CustomerId);
        await db.Orders.AddAsync(order);
        await db.SaveChangesAsync();

        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();

        // Act
        var response = await client.DeleteAsync($"{OrderManagement}/{orderId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var updatedOrder = await db.Orders.Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        Assert.That(updatedOrder, Is.Not.Null);
        Assert.That(updatedOrder.StatusHistory.Last().Status, Is.EqualTo(nameof(Status.Cancelled)));
        
        _testEnvironment.EventConsumer!.ConsumeWithTimeout<OrderEventDto>(Topics.OrderEvents, TimeSpan.FromSeconds(1));
        Assert.That(_testEnvironment.EventConsumer!.HasMessageContaining<OrderEventDto>(o =>
            o is { EventType: nameof(Events.OrderCancelled), Order.OrderId: orderId }), Is.True);
    }
    
    [Test]
    public async Task CancelOrder_OrderDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        const string orderId = "non-existent-order";
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();

        // Act
        var response = await client.DeleteAsync($"{OrderManagement}/{orderId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task CancelOrder_OrderBelongsToAnotherCustomer_ReturnsNotFound()
    {
        // Arrange
        const string orderId = "order-belongs-to-someone-else";

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = CreateTestOrder(orderId, "other-customer");
        await db.Orders.AddAsync(order);
        await db.SaveChangesAsync();

        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();

        // Act
        var response = await client.DeleteAsync($"{OrderManagement}/{orderId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
            "Customer should not be able to cancel another customer's order");
    }

    private static Order CreateTestOrder(string orderId, string customerId)
    {
        var customer = new CustomerDetails
        {
            OrderId = orderId,
            CustomerId = customerId,
            FullName = "John Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "+123456789"
        };
        var items = new List<OrderItem>
        {
            new OrderItem
            {
                ProductId = Guid.NewGuid().ToString(),
                Quantity = 2,
                UnitPrice = 49.99m
            },
            new OrderItem
            {
                ProductId = Guid.NewGuid().ToString(),
                Quantity = 1,
                UnitPrice = 99.50m
            }
        };

        var statusHistory = new List<OrderStatus>
        {
            new OrderStatus
            {
                OrderId = orderId,
                Status = nameof(Status.Pending)
            }
        };

        var shippingAddress = new Address
        {
            Country = "USA",
            State = "California",
            City = "Los Angeles",
            PostalCode = "90001",
            AddressLine = "123 Main Street"
        };

        var billingAddress = new Address
        {
            Country = "USA",
            State = "California",
            City = "Los Angeles",
            PostalCode = "90001",
            AddressLine = "123 Main Street"
        };

        return new Order
        {
            Id = orderId,
            CheckoutId = Guid.NewGuid().ToString(),
            CustomerDetails = customer,
            OrderItems = items,
            StatusHistory = statusHistory,
            ShippingAddress = shippingAddress,
            BillingAddress = billingAddress,
            PaymentMethod = "CreditCard",
            ShippingMethod = "Standard",
            PaymentIntentId = null,
            ShippingFee = 10.00m,
            PaymentFee = 2.50m,
            CustomerNotes = "Leave at front door",
            EstimatedDeliveryDate = DateTime.UtcNow.AddDays(5)
        };
    }
    
    private CartCheckedOutEventDto CreateValidCartEvent(string checkoutId)
    {
        return new CartCheckedOutEventDto
        {
            EventType = (nameof(Events.CartCheckedOut)),
            EventId = Guid.NewGuid().ToString(),    
            CheckoutId = checkoutId,
            CustomerId = CustomerId,
            PaymentMethod = "CreditCard",
            ShippingMethod = "Standard",
            CustomerDetails = new CustomerDetailsDto("John Doe", "john@example.com", "555-1234"),
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
}
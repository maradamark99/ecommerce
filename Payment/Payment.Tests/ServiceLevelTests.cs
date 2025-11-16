using System.Net;
using System.Net.Http.Json;
using EcommerceLib.Contract.Events;
using EcommerceLib.Testing.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payment.Model;

namespace Payment.Tests;

public class ServiceLevelTests
{
    private TestEnvironment<ITestEnvironmentMarker> _testEnvironment;
    private const string Payments = "/api/v1/payments";
    private const string CustomerId = "customer1";
    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _testEnvironment = TestEnvironment<ITestEnvironmentMarker>.Builder
            .WithDatabase<AppDbContext>()
            .WithMockServer()
            .WithProducer()
            .WithConsumer()
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
    public async Task InitiatePayment_InvalidPaymentMethod_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        
        // Act
        var response = await client.PostAsync(Payments + "/initiate-payment", JsonContent.Create(new { OrderId = "order123", PaymentMethod = "invalid" }));
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
    
    [Test]
    public async Task InitiatePayment_OrderIdIsMissing_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        
        // Act
        var response = await client.PostAsync(Payments + "/initiate-payment", JsonContent.Create(new { OrderId = "", PaymentMethod = "CashOnDelivery" }));
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
    
    [Test]
    public async Task InitiatePayment_NoOrderFoundForCustomer_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        
        // Act
        var response = await client.PostAsync(Payments + "/initiate-payment", JsonContent.Create(new { OrderId = "order123", PaymentMethod = "CashOnDelivery" }));
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
    
    [Test]
    public async Task InitiatePayment_PaymentMethodInRequestDoesNotEqualPaymentMethodOfOrder_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = new Order()
        {
            CustomerId = CustomerId,
            OrderId = "order123",
            SelectedPaymentMethod = PaymentMethod.CashOnDelivery,
            TotalAmount = 34.99m
        };
        await db.Orders.AddAsync(order);
        await db.SaveChangesAsync();
        
        // Act
        var response = await client.PostAsync(Payments + "/initiate-payment", JsonContent.Create(new { OrderId = "order123", PaymentMethod = "CreditCard" }));
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
    
    [Test]
    public async Task InitiatePayment_HappyCase()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var paymentMethod = "CashOnDelivery";
        var orderId = "order123";
        var order = new Order()
        {
            CustomerId = CustomerId,
            OrderId = orderId,
            TotalAmount = 34.99m,
            SelectedPaymentMethod = PaymentMethod.CashOnDelivery
        };
        await db.Orders.AddAsync(order);
        await db.SaveChangesAsync();
        
        // Act
        var response = await client.PostAsync(Payments + "/initiate-payment", JsonContent.Create(new { OrderId = "order123", PaymentMethod = paymentMethod }));
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var payment = await db.Payments
            .AsNoTracking()
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p =>
                p.Order.CustomerId == CustomerId && p.Order.OrderId == orderId);

        _testEnvironment.EventConsumer!.ConsumeWithTimeout<PaymentEventDto>(Topics.PaymentEvents,
            TimeSpan.FromSeconds(3));
        _testEnvironment.EventConsumer.HasMessageContaining<PaymentEventDto>(e =>
            e.EventType == nameof(Events.PaymentPendingOnDelivery) &&
            e.CustomerId == CustomerId &&
            e.OrderId == orderId &&
            e.Amount == 34.99m);
            
        Assert.That(payment, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(payment.Order.SelectedPaymentMethod.ToString(), Is.EqualTo(paymentMethod));
            Assert.That(payment.Status, Is.EqualTo(nameof(PaymentStatus.PendingOnDelivery)));
        });
    }
    
    [Test]
    public async Task OrderCreated_ShouldSaveOrderInDatabase()
    {
        // Arrange
        const string orderId = "order123";
        var orderCreatedEventDto = CreateOrderEventDto(orderId, nameof(Events.OrderCreated));
        
        // Act
        await _testEnvironment.EventProducer!.ProduceAsync(Topics.OrderEvents, orderCreatedEventDto,
            Guid.NewGuid().ToString());
        await Task.Delay(TimeSpan.FromSeconds(1));
        
        // Assert
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var order = await db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.CustomerId == CustomerId && c.OrderId == orderId);
            
        Assert.That(order, Is.Not.Null);
    }
    
    [Test]
    public async Task OrderCompleted_PaymentStatusIsNotPaid_ShouldUpdatePaymentStatus()
    {
        // Arrange
        const string orderId = "order123";
        const string paymentId = "payment123";
        const PaymentMethod paymentMethod = PaymentMethod.CashOnDelivery;
        var orderCompletedEventDto = CreateOrderEventDto(orderId, nameof(Events.OrderCompleted));
        
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var order = new Order()
        {
            CustomerId = CustomerId,
            OrderId = orderId,
            TotalAmount = 34.99m,
            SelectedPaymentMethod = paymentMethod
        };
        var payment = new Model.Payment()
        {
            Id = paymentId,
            Order = order,
            Status = nameof(PaymentStatus.PendingOnDelivery),
        };
        await db.Payments.AddAsync(payment);
        await db.SaveChangesAsync();
        
        // Act
        await _testEnvironment.EventProducer!.ProduceAsync(Topics.OrderEvents, orderCompletedEventDto,
            Guid.NewGuid().ToString());
        await Task.Delay(TimeSpan.FromSeconds(1));
        
        // Assert
        var updatedPayment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == payment.Id);
        Assert.That(updatedPayment, Is.Not.Null);
        Assert.That(updatedPayment.Status, Is.EqualTo(nameof(PaymentStatus.Paid)));
    }

    private static object CreateOrderEventDto(string orderId, string eventType)
    {
        return new
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = eventType,
            CorrelationId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Order = new
            {
                OrderId = orderId,
                Customer = new
                {
                    CustomerId = CustomerId,
                    FullName = "John Doe",
                    ShippingAddress = new
                    {
                        AddressLine = "123 Main St",
                        City = "New York",
                        State = "NY",
                        PostalCode = "10001",
                        Country = "USA"
                    },
                    BillingAddress = new
                    {
                        AddressLine = "456 Elm St",
                        City = "New York",
                        State = "NY",
                        PostalCode = "10001",
                        Country = "USA"
                    }
                },
                Items = new[]
                {
                    new
                    {
                        ProductId = "PROD-001",
                        Name = "Widget A",
                        Quantity = 2,
                        Price = 19.99m,
                        Condition = "New"
                    },
                    new
                    {
                        ProductId = "PROD-002",
                        Name = "Gadget B",
                        Quantity = 1,
                        Price = 49.99m,
                        Condition = "New"
                    }
                },
                TotalAmount = 89.97m,
                StatusHistory = new List<string> { "Completed" },
                CreatedAt = DateTime.UtcNow.AddMinutes(-2),
                ShippingMethod = "UPS Ground",
                PaymentMethod = "CashOnDelivery"
            }
        };
    }
}
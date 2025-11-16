using System.Net;
using System.Net.Http.Json;
using EcommerceLib.Contract;
using EcommerceLib.Contract.Dto;
using EcommerceLib.Contract.Events;
using EcommerceLib.Testing.Infrastructure;

namespace Shipping.Tests;

public class ServiceLevelTests
{
    private TestEnvironment<ITestEnvironmentMarker> _testEnvironment;
    private const string Shipping = "/api/v1/shipping";
    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _testEnvironment = TestEnvironment<ITestEnvironmentMarker>.Builder
            .WithConsumer()
            .WithProducer()
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
    public async Task GetShippingRates_WithValidShippingMethod_ShouldReturnShippingRate()
    {
        // Arrange
        const string validShippingMethod = nameof(ShippingMethod.Standard);
        using var client = _testEnvironment.CreateClient();

        // Act
        var response = await client.GetAsync(Shipping + $"/fee/{validShippingMethod}");
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Is.Not.Null);
        });
        var shipmentRate = await response.Content.ReadFromJsonAsync<ShippingRateDto>();
        Assert.That(shipmentRate, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(shipmentRate.Method, Is.EqualTo(validShippingMethod));
            Assert.That(shipmentRate.Fee, Is.GreaterThan(0));
        });
    }
    
    [Test]
    public async Task GetShippingRates_WithInvalidShippingMethod_ShouldReturnBadRequest()
    {
        // Arrange
        const string invalidShippingRate = "InvalidShippingRate";
        using var client = _testEnvironment.CreateClient();

        // Act
        var response = await client.GetAsync(Shipping + $"/fee/{invalidShippingRate}");
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest)); 
    }

    [Test]
    public async Task FulfillOrderIsEmitted_ShouldCreateShipment()
    {
        var testOrderEvent = CreateTestOrderEvent();
        await _testEnvironment.EventProducer!.ProduceAsync(Topics.OrderEvents, testOrderEvent, Guid.NewGuid().ToString());
        await Task.Delay(TimeSpan.FromSeconds(1));

        _testEnvironment.EventConsumer!.ConsumeWithTimeout<ShippingEventDto>(Topics.ShippingEvents,
            TimeSpan.FromSeconds(3));
        _testEnvironment.EventConsumer!.HasMessageContaining<ShippingEventDto>(s => 
            s.EventType == nameof(Events.ShipmentCreated) 
            && s.OrderId == testOrderEvent.Order.OrderId 
            && s.Customer.CustomerId == testOrderEvent.Order.Customer.CustomerId);
        
        await Task.Delay(TimeSpan.FromSeconds(2));
        _testEnvironment.EventConsumer!.ConsumeWithTimeout<ShippingEventDto>(Topics.ShippingEvents,
            TimeSpan.FromSeconds(3));
        _testEnvironment.EventConsumer!.HasMessageContaining<ShippingEventDto>(s => 
            s.EventType == nameof(Events.ShipmentDelivered) 
            && s.OrderId == testOrderEvent.Order.OrderId 
            && s.Customer.CustomerId == testOrderEvent.Order.Customer.CustomerId);
    }

    private static OrderEventDto CreateTestOrderEvent()
    {
        return new OrderEventDto
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.FulfillOrder),
            CorrelationId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Order = new OrderDto
            {
                OrderId = "ORD-1001",
                Customer = new CustomerDto
                {
                    CustomerId = "CUST-500",
                    FullName = "Jane Doe",
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
                        AddressLine = "123 Main St",
                        City = "Springfield",
                        State = "IL",
                        PostalCode = "62704",
                        Country = "USA"
                    }
                },
                Items = new List<ItemDto>
                {
                    new ItemDto
                    {
                        ProductId = "SKU-12345",
                        Name = "Wireless Mouse",
                        Quantity = 1,
                        Price = 29.99m,
                        Condition = "New"
                    },
                    new ItemDto
                    {
                        ProductId = "SKU-67890",
                        Name = "Mechanical Keyboard",
                        Quantity = 1,
                        Price = 79.99m,
                        Condition = "New"
                    }
                },
                TotalAmount = 109.98m,
                StatusHistory = new List<string>
                {
                    "Created",
                    "Confirmed"
                },
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                ShippingMethod = "Standard",
                PaymentMethod = "CreditCard"
            }
        };
    }
}
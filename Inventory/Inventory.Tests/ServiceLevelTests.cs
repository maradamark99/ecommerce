using System.Net;
using System.Net.Http.Json;
using EcommerceLib;
using EcommerceLib.Auth;
using EcommerceLib.Contract.Events;
using EcommerceLib.Testing.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Tests;

public class ServiceLevelTests
{
    private TestEnvironment<ITestEnvironmentMarker> _testEnvironment;
    private const string Inventory = "/api/v1/inventory";
    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _testEnvironment = TestEnvironment<ITestEnvironmentMarker>.Builder
            .WithDatabase<AppDbContext>()
            .WithMockServer()
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
    public async Task ProductCreatedEventEmitted_ShouldCreateInventoryEntry()
    {
        // Arrange
        const string productId = "prod-001"; 
        var productCreatedEvent = new
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.ProductCreated),
            ProductId = productId
        };
        
        // Act
        await _testEnvironment.EventProducer!.ProduceAsync(Topics.ProductEvents, productCreatedEvent, Guid.NewGuid().ToString());
        await Task.Delay(TimeSpan.FromSeconds(1));
        
        // Assert
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entry = await db.InventoryEntries.FirstOrDefaultAsync(i => i.ProductId == productId);
        Assert.That(entry, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(entry.AvailableQuantity, Is.EqualTo(0));
            Assert.That(entry.ProductId, Is.EqualTo(productId));
        });
    }
    
    [Test]
    public async Task TryReserveStock_HappyCase()
    {
        // Arrange
        using var client = _testEnvironment.CreateClient();
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        const string orderId = "order-001";
        const string product1Id = "P123";
        const int prod1Quantity = 4;
        const string product2Id = "P456";
        const int prod2Quantity = 2;
       
        var inventoryEntry1 = new InventoryEntry
        {
            Id = Guid.NewGuid().ToString(), 
            ProductId = product1Id,
            AvailableQuantity = prod1Quantity
        };
        var inventoryEntry2 = new InventoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            ProductId = product2Id,
            AvailableQuantity = prod2Quantity
        };
        await db.InventoryEntries.AddRangeAsync(inventoryEntry1, inventoryEntry2);
        await db.SaveChangesAsync();
        // Act
        var request = new
        {
            OrderId = orderId, 
            Items = new object[]
            {
                new { ProductId = product1Id, Quantity = 3 },
                new { ProductId = product2Id, Quantity = 1 },
            }
        };
        var response = await client.PostAsync(Inventory + "/reserve-stock", JsonContent.Create(request));
        
        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content, Is.Not.Null);
        var reservationResults = await response.Content.ReadFromJsonAsync<List<ReservationResult>>();
        Assert.That(reservationResults, Is.Not.Null);
        Assert.That(reservationResults, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(reservationResults, Has.Count.EqualTo(2));
            Assert.That(reservationResults.All(r => r.OrderId == orderId), Is.True);
            Assert.That(reservationResults.All(r => r.IsSuccess), Is.True);

            var productIds = reservationResults.Select(r => r.ProductId).ToList();
            Assert.That(productIds, Does.Contain(product1Id));
            Assert.That(productIds, Does.Contain(product2Id));
        });
        
        var reservationsInDb = await db.StockReservations
            .AsNoTracking()
            .Where(r => r.OrderId == orderId)
            .ToListAsync();
        Assert.That(reservationsInDb, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(reservationsInDb.Any(r => r is { ProductId: product1Id, Quantity: 3 }), Is.True);
            Assert.That(reservationsInDb.Any(r => r is { ProductId: product2Id, Quantity: 1 }), Is.True);
        });
        var updatedEntry1 = await db.InventoryEntries.AsNoTracking().FirstAsync(e => e.ProductId == product1Id);
        var updatedEntry2 = await db.InventoryEntries.AsNoTracking().FirstAsync(e => e.ProductId == product2Id);

        Assert.Multiple(() =>
        {
            Assert.That(updatedEntry1.AvailableQuantity, Is.EqualTo(prod1Quantity - 3));
            Assert.That(updatedEntry2.AvailableQuantity, Is.EqualTo(prod2Quantity - 1));
        });
    }

    [Test]
    public async Task TryReserveStock_Failure_RollsBackAllReservations()
    {
        // Arrange
        using var client = _testEnvironment.CreateClient();
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        const string orderId = "order-002";
        const string product1Id = "P123";
        const string product2Id = "P456";
        const int prod1Quantity = 4;
        const int prod2Quantity = 1;

        var inventoryEntry1 = new InventoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            ProductId = product1Id,
            AvailableQuantity = prod1Quantity
        };
        var inventoryEntry2 = new InventoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            ProductId = product2Id,
            AvailableQuantity = prod2Quantity
        };

        await db.InventoryEntries.AddRangeAsync(inventoryEntry1, inventoryEntry2);
        await db.SaveChangesAsync();

        // Act
        var request = new
        {
            OrderId = orderId,
            Items = new object[]
            {
                new { ProductId = product1Id, Quantity = 3 },
                new { ProductId = product2Id, Quantity = 2 } 
            }
        };

        var response = await client.PostAsync(Inventory + "/reserve-stock", JsonContent.Create(request));

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var reservationResults = await response.Content.ReadFromJsonAsync<List<ReservationResult>>();
        Assert.That(reservationResults, Is.Not.Null);
        Assert.That(reservationResults, Has.Count.EqualTo(2));

        Assert.Multiple(async () =>
        {
            Assert.That(reservationResults.Any(r => r is { ProductId: product1Id, IsSuccess: true }), Is.True);
            Assert.That(reservationResults.Any(r => r is { ProductId: product2Id, IsSuccess: false }), Is.True);


            var reservationsInDb = await db.StockReservations
                .AsNoTracking()
                .Where(r => r.OrderId == orderId)
                .ToListAsync();
            Assert.That(reservationsInDb, Is.Empty);

            var updatedEntry1 = await db.InventoryEntries.AsNoTracking().FirstAsync(e => e.ProductId == product1Id);
            var updatedEntry2 = await db.InventoryEntries.AsNoTracking().FirstAsync(e => e.ProductId == product2Id);

            Assert.Multiple(() =>
            {
                Assert.That(updatedEntry1.AvailableQuantity, Is.EqualTo(prod1Quantity));
                Assert.That(updatedEntry2.AvailableQuantity, Is.EqualTo(prod2Quantity));
            });
        });
    }
    
    [Test]
    public async Task OrderCancelledEvent_ShouldReleaseReservedStock()
    {
        // Arrange
        const string orderId = "order-005";
        const string productId = "P123";
        const int initialQuantity = 5;

        // Seed product, inventory, and reservation
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var inventoryEntry = new InventoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            ProductId = productId,
            AvailableQuantity = initialQuantity - 3
        };
        var reservation = new StockReservation
        {
            OrderId = orderId,
            ProductId = productId,
            Quantity = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        await db.InventoryEntries.AddAsync(inventoryEntry);
        await db.StockReservations.AddAsync(reservation);
        await db.SaveChangesAsync();

        // Create event payload
        var orderCancelledEvent = new
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.OrderCancelled),
            CorrelationId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Order = new
            {
                OrderId = orderId,
                StatusHistory = new[] { "Created", "Cancelled" },
                Items = new object[] { new { ProductId = productId, Quantity = 3 } }
            }
        };

        await _testEnvironment.EventProducer!.ProduceAsync(
            Topics.OrderEvents, 
            orderCancelledEvent, 
            Guid.NewGuid().ToString()
        );

        await Task.Delay(TimeSpan.FromSeconds(1));

        // Assert
        var reservationInDb = await db.StockReservations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrderId == orderId && r.ProductId == productId);
        Assert.That(reservationInDb, Is.Null);

        var inventoryEntryUpdated = await db.InventoryEntries
            .AsNoTracking()
            .FirstAsync(e => e.ProductId == productId);
        Assert.That(inventoryEntryUpdated.AvailableQuantity, Is.EqualTo(initialQuantity));
    }
    
    [Test]
    public async Task OrderExpiredEvent_ShouldReleaseReservedStock()
    {
        // Arrange
        const string orderId = "order-006";
        const string productId = "P123";
        const int initialQuantity = 5;

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var inventoryEntry = new InventoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            ProductId = productId,
            AvailableQuantity = initialQuantity - 3
        };
        var reservation = new StockReservation
        {
            OrderId = orderId,
            ProductId = productId,
            Quantity = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        await db.InventoryEntries.AddAsync(inventoryEntry);
        await db.StockReservations.AddAsync(reservation);
        await db.SaveChangesAsync();

        var orderExpiredEvent = new
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.OrderExpired),
            CorrelationId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Order = new
            {
                OrderId = orderId,
                StatusHistory = new[] { "Pending", "Created" },
                Items = new object[] { new { ProductId = productId, Quantity = 3 } }
            }
        };

        // Act
        await _testEnvironment.EventProducer!.ProduceAsync(
            Topics.OrderEvents,
            orderExpiredEvent,
            Guid.NewGuid().ToString()
        );

        await Task.Delay(TimeSpan.FromSeconds(1));

        var reservationInDb = await db.StockReservations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrderId == orderId && r.ProductId == productId);
        Assert.That(reservationInDb, Is.Null, "Reservation should be released after order expired event");

        var inventoryEntryUpdated = await db.InventoryEntries
            .AsNoTracking()
            .FirstAsync(e => e.ProductId == productId);
        Assert.That(inventoryEntryUpdated.AvailableQuantity, Is.EqualTo(initialQuantity));
    }
    
    [Test]
    public async Task UpdateStock_HappyCase()
    {
        // Arrange
        const string productId = "P123";
        const int initialQuantity = 5;
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId("admin")
            .WithRoles(Roles.Admin)
            .Build();

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var inventoryEntry = new InventoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            ProductId = productId,
            AvailableQuantity = initialQuantity
        };

        await db.InventoryEntries.AddAsync(inventoryEntry);
        await db.SaveChangesAsync();

        // Act && Assert
        await client.PutAsync(Inventory + "/update-stock", JsonContent.Create(new { ProductId = productId, Quantity = 4, Operation = "Add" }));

        var invEntryAfterAddition = await db.InventoryEntries.AsNoTracking().FirstAsync(i => i.ProductId == productId);
        Assert.That(invEntryAfterAddition.AvailableQuantity, Is.EqualTo(initialQuantity + 4));
        
        await client.PutAsync(Inventory + "/update-stock", JsonContent.Create(new { ProductId = productId, Quantity = 2, Operation = "Subtract" }));
        
        var invEntryAfterSubtraction = await db.InventoryEntries.AsNoTracking().FirstAsync(i => i.ProductId == productId);
        Assert.That(invEntryAfterSubtraction.AvailableQuantity, Is.EqualTo(initialQuantity + 2));
    }
    
    [Test]
    public async Task UpdateStock_InvalidOperation_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId("admin")
            .WithRoles(Roles.Admin)
            .Build();

        // Act
        var response = await client.PutAsync(Inventory + "/update-stock", JsonContent.Create(new { ProductId = "productId", Quantity = 4, Operation = "Invalid" }));
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
    
}
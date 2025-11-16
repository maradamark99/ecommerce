using Inventory.Contract;
using Moq;

namespace Inventory.Tests;

public class BusinessLogicTests
{
    private Mock<IInventoryRepository> _mockRepo = null!;
    private InventoryService _service = null!;
    private InventoryEntry? _capturedEntry;

    [SetUp]
    public void SetUp()
    {
        _capturedEntry = null;

        _mockRepo = new Mock<IInventoryRepository>();
        _mockRepo
            .Setup(r => r.CreateInventoryEntryAsync(It.IsAny<InventoryEntry>()))
            .Callback<InventoryEntry>(entry => _capturedEntry = entry)
            .Returns(Task.CompletedTask);

        _service = new InventoryService(_mockRepo.Object);
    }

    [Test]
    public async Task CreateInventoryEntryForProductAsync_ShouldCreateEntryWithCorrectValues()
    {
        // Arrange
        var productId = "prod-123";
        var initialStock = 10;

        // Act
        await _service.CreateInventoryEntryForProductAsync(productId, initialStock);

        // Assert
        _mockRepo.Verify(r => r.CreateInventoryEntryAsync(It.IsAny<InventoryEntry>()), Times.Once);

        Assert.That(_capturedEntry, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(_capturedEntry!.Id, Is.Not.Empty);
            Assert.That(_capturedEntry.ProductId, Is.EqualTo(productId));
            Assert.That(_capturedEntry.AvailableQuantity, Is.EqualTo(initialStock));
            Assert.That(_capturedEntry.IsReorderNeeded, Is.False);
            Assert.That(_capturedEntry.CreatedAt, Is.InRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow));
            Assert.That(_capturedEntry.UpdatedAt, Is.InRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow));
        });
    }

    [Test]
    public void CreateInventoryEntryForProductAsync_WhenRepositoryThrows_ShouldPropagateException()
    {
        // Arrange
        _mockRepo.Reset(); 
        _mockRepo
            .Setup(r => r.CreateInventoryEntryAsync(It.IsAny<InventoryEntry>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.CreateInventoryEntryForProductAsync("prod-123"));
    }
    
    [Test]
    public async Task ReturnsTrue_WhenEntryExistsAndHasEnoughStock()
    {
        // Arrange
        var productId = "prod-123";
        var entry = new InventoryEntry { ProductId = productId , AvailableQuantity = 10 };

        _mockRepo
            .Setup(r => r.GetInventoryEntryByProductIdAsync(productId))
            .ReturnsAsync(entry);

        // Act
        var result = await _service.IsProductInStockAsync(productId, 5);

        // Assert
        Assert.That(result, Is.True);
        _mockRepo.Verify(r => r.GetInventoryEntryByProductIdAsync(productId), Times.Once);
    }

    [Test]
    public async Task ReturnsFalse_WhenEntryExistsButNotEnoughStock()
    {
        // Arrange
        var productId = "prod-123";
        var entry = new InventoryEntry { ProductId = productId, AvailableQuantity = 2 };

        _mockRepo
            .Setup(r => r.GetInventoryEntryByProductIdAsync(productId))
            .ReturnsAsync(entry);

        // Act
        var result = await _service.IsProductInStockAsync(productId, 5);

        // Assert
        Assert.That(result, Is.False);
        _mockRepo.Verify(r => r.GetInventoryEntryByProductIdAsync(productId), Times.Once);
    }

    [Test]
    public async Task ReturnsFalse_WhenEntryDoesNotExist()
    {
        // Arrange
        var productId = "prod-123";
        _mockRepo
            .Setup(r => r.GetInventoryEntryByProductIdAsync(productId))
            .ReturnsAsync((InventoryEntry?)null);

        // Act
        var result = await _service.IsProductInStockAsync(productId, 1);

        // Assert
        Assert.That(result, Is.False);
        _mockRepo.Verify(r => r.GetInventoryEntryByProductIdAsync(productId), Times.Once);
    }
    
    [Test]
    public async Task ReturnsSuccessResults_WhenAllReservationsSucceed()
    {
        // Arrange
        var request = new StockReservationRequest
        (
            OrderId: "order-123",
            Items:
            [
                new(ProductId: "p1", Quantity: 1 ),
                new( ProductId: "p2", Quantity: 2 )
            ]
        );

        _mockRepo
            .Setup(r => r.TryReserveStockAsync(It.IsAny<StockReservation>()))
            .ReturnsAsync(true);

        // Act
        var results = await _service.TryReserveStockAsync(request);

        // Assert
        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.All(r => r.IsSuccess), Is.True);

        _mockRepo.Verify(r => r.TryReserveStockAsync(It.IsAny<StockReservation>()), Times.Exactly(2));
        _mockRepo.Verify(r => r.GetStockReservationByOrderIdAsync(It.IsAny<string>()), Times.Never);
        _mockRepo.Verify(r => r.TryReleaseReservedStockAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task RollsBack_WhenAnyReservationFails()
    {
        // Arrange
        var request = new StockReservationRequest
        (
            OrderId: "order-123",
            Items:
            [
                new(ProductId: "p1", Quantity: 1 ),
                new( ProductId: "p2", Quantity: 2 )
            ]
        );
        
        _mockRepo
            .SetupSequence(r => r.TryReserveStockAsync(It.IsAny<StockReservation>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mockRepo
            .Setup(r => r.GetStockReservationByOrderIdAsync(request.OrderId))
            .ReturnsAsync(new List<StockReservation>
            {
                new() { ProductId = "p1", ExpiresAt = DateTime.UtcNow.AddMinutes(10) }
            });

        _mockRepo
            .Setup(r => r.TryReleaseReservedStockAsync(request.OrderId, "p1"))
            .ReturnsAsync(true);

        // Act
        var results = await _service.TryReserveStockAsync(request);

        // Assert
        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results.Any(r => !r.IsSuccess), Is.True);

        _mockRepo.Verify(r => r.TryReserveStockAsync(It.IsAny<StockReservation>()), Times.Exactly(2));

        _mockRepo.Verify(r => r.GetStockReservationByOrderIdAsync(request.OrderId), Times.Once);
        _mockRepo.Verify(r => r.TryReleaseReservedStockAsync(request.OrderId, "p1"), Times.Once);
    }
    
    [Test]
    public async Task CallsRepositoryWithPositiveQuantity_WhenOperationIsAdd()
    {
        // Arrange
        const string productId = "p1";
        const int quantity = 5;

        _mockRepo
            .Setup(r => r.UpdateStockAsync(productId, quantity))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateStockAsync(productId, quantity, Operation.Add);

        // Assert
        _mockRepo.Verify(r => r.UpdateStockAsync(productId, quantity), Times.Once);
    }

    [Test]
    public async Task CallsRepositoryWithNegativeQuantity_WhenOperationIsSubtract()
    {
        // Arrange
        const string productId = "p1";
        const int quantity = 5;

        _mockRepo
            .Setup(r => r.UpdateStockAsync(productId, -quantity))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateStockAsync(productId, quantity, Operation.Subtract);

        // Assert
        _mockRepo.Verify(r => r.UpdateStockAsync(productId, -quantity), Times.Once);
    }

    [Test]
    public void ThrowsArgumentOutOfRange_WhenOperationIsUnknown()
    {
        // Arrange
        const string productId = "p1";
        const int quantity = 5;
        const Operation invalidOperation = (Operation)999;

        // Act + Assert
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await _service.UpdateStockAsync(productId, quantity, invalidOperation));
        
        _mockRepo.Verify(r => r.UpdateStockAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }
    
}
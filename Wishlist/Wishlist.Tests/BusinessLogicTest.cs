using EcommerceLib.Auth;
using EcommerceLib.Contract.Events;
using EcommerceLib.Exception;
using EcommerceLib.Messaging;
using Moq;
using Wishlist.Contract;

namespace Wishlist.Tests;

[TestFixture]
public class BusinessLogicTests
{
    private Mock<IWishlistRepository> _wishlistRepositoryMock;
    private Mock<IProductManagementClient> _productManagementClientMock;
    private Mock<IEventProducer<WishlistEventDto>> _eventProducerMock;
    private WishlistService _wishlistService;

    [SetUp]
    public void Setup()
    {
        _wishlistRepositoryMock = new Mock<IWishlistRepository>();
        _productManagementClientMock = new Mock<IProductManagementClient>();
        _eventProducerMock = new Mock<IEventProducer<WishlistEventDto>>();
        _wishlistService = new WishlistService(_productManagementClientMock.Object, _wishlistRepositoryMock.Object,
            _eventProducerMock.Object);
    }

    [Test]
    public async Task GetWishlistAsync_ReturnsCorrectItems()
    {
        // Arrange
        var customer = new AppUser { Id = "customer123" };
        var wishlist = new Wishlist
        {
            Id = "wishlist123",
            CustomerId = customer.Id,
            Products = new List<Product>
            {
                new()
                {
                    Id = "product1", Name = "Product 1", Price = 10, IsAvailable = true, AddedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = "product2", Name = "Product 2", Price = 20, IsAvailable = false, AddedAt = DateTime.UtcNow
                }
            }
        };
        _wishlistRepositoryMock.Setup(r => r.GetWishlistForCustomerAsync(It.IsAny<string>())).ReturnsAsync(wishlist);

        // Act
        var result = (await _wishlistService.GetWishlistAsync(customer))?.ToList();


        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Id, Is.EqualTo("product1"));
            Assert.That(result[0].Name, Is.EqualTo("Product 1"));
            Assert.That(result[0].Price, Is.EqualTo(10));
            Assert.That(result[0].IsAvailable, Is.True);
        });
        Assert.Multiple(() =>
        {
            Assert.That(result[1].Id, Is.EqualTo("product2"));
            Assert.That(result[1].Name, Is.EqualTo("Product 2"));
            Assert.That(result[1].Price, Is.EqualTo(20));
            Assert.That(result[1].IsAvailable, Is.False);
        });
    }

    [Test]
    public async Task GetWishlistAsync_WishlistDoesNotExist_ReturnsNull()
    {
        // Arrange
        var customer = new AppUser { Id = "customer123" };
        _wishlistRepositoryMock.Setup(r => r.GetWishlistForCustomerAsync(It.IsAny<string>())).ReturnsAsync(default(Wishlist));

        // Act
        var result = await _wishlistService.GetWishlistAsync(customer);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void AddToWishlistAsync_ProductNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var customer = new AppUser { Id = "customer123" };
        const string productId = "product123";
        _productManagementClientMock.Setup(c => c.GetProductByIdAsync(It.IsAny<string>())).ReturnsAsync(default(ProductDto));

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(() => _wishlistService.AddToWishlistAsync(customer, productId));
    }

    [Test]
    public async Task AddToWishlistAsync_WishlistDoesNotExist_CreatesWishlist()
    {
        // Arrange
        var customer = new AppUser { Id = "customer123" };
        const string productId = "product123";
        var productCatalogResponse = new ProductDto(productId, "Product 1", 10, "example.com/abcd", true);
        _productManagementClientMock.Setup(c => c.GetProductByIdAsync(It.IsAny<string>())).ReturnsAsync(productCatalogResponse);
        _wishlistRepositoryMock.Setup(r => r.GetWishlistForCustomerAsync(It.IsAny<string>())).ReturnsAsync(default(Wishlist));
        _wishlistRepositoryMock.Setup(r => r.CreateWishlistAsync(It.IsAny<string>(), It.IsAny<Product>())).Returns(Task.CompletedTask);

        // Act
        await _wishlistService.AddToWishlistAsync(customer, productId);

        // Assert
        _wishlistRepositoryMock.Verify(r => r.CreateWishlistAsync(It.IsAny<string>(), It.IsAny<Product>()), Times.Once);
    }

    [Test]
    public async Task AddToWishlistAsync_WishlistExists_AddsToWishlist()
    {
        // Arrange
        var customer = new AppUser { Id = "customer123" };
        const string productId = "product123";
        var productCatalogResponse = new ProductDto(productId, "Product 1", 10, "example.com/abcd", true);
        var wishlist = new Wishlist()
        {
            CustomerId = customer.Id
        };
        _productManagementClientMock.Setup(c => c.GetProductByIdAsync(It.IsAny<string>())).ReturnsAsync(productCatalogResponse);
        _wishlistRepositoryMock.Setup(r => r.GetWishlistForCustomerAsync(It.IsAny<string>())).ReturnsAsync(wishlist);
        _wishlistRepositoryMock.Setup(r => r.CreateWishlistAsync(It.IsAny<string>(), It.IsAny<Product>())).Returns(Task.CompletedTask);

        // Act
        await _wishlistService.AddToWishlistAsync(customer, productId);

        // Assert
        _wishlistRepositoryMock.Verify(r => r.AddToWishlistAsync(It.IsAny<Wishlist>(), It.IsAny<Product>()), Times.Once);
    }

    [Test]
    public void RemoveFromWishlistAsync_WishlistDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var customer = new AppUser { Id = "customer123" };
        const string productId = "product123";
        _wishlistRepositoryMock.Setup(r => r.GetWishlistForCustomerAsync(It.IsAny<string>())).ReturnsAsync(default(Wishlist));

        Assert.ThrowsAsync<NotFoundException>(() => _wishlistService.RemoveFromWishlistAsync(customer, productId));
    }

    [Test]
    public async Task RemoveFromWishlistAsync_WishlistExists_RemovesFromWishlist()
    {
        // Arrange
        var customer = new AppUser { Id = "customer123" };
        const string productId = "product123";
        var wishlist = new Wishlist()
        {
            CustomerId = customer.Id
        };
        _wishlistRepositoryMock.Setup(r => r.GetWishlistForCustomerAsync(It.IsAny<string>())).ReturnsAsync(wishlist);
        _wishlistRepositoryMock.Setup(r => r.RemoveFromWishlistAsync(It.IsAny<Wishlist>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        await _wishlistService.RemoveFromWishlistAsync(customer, productId);

        // Assert
        _wishlistRepositoryMock.Verify(r => r.RemoveFromWishlistAsync(It.IsAny<Wishlist>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task UpdateProductAsync_ProductDoesNotExist_DoesNothing()
    {
        // Arrange
        var msg = new ProductEventDto { ProductId = "product123" };
        _wishlistRepositoryMock.Setup(r => r.GetProductByIdAsync(It.IsAny<string>())).ReturnsAsync(default(Product));

        // Act
        await _wishlistService.UpdateProductAsync(msg);

        // Assert
        _wishlistRepositoryMock.Verify(r => r.UpdateProductAsync(It.IsAny<Product>()), Times.Never);
    }

    [Test]
    public async Task UpdateProductAsync_ProductExists_UpdatesProduct()
    {
        // Arrange
        var msg = new ProductEventDto { ProductId = "product123" };
        var product = new Product { Id = "product123" };
        _wishlistRepositoryMock.Setup(r => r.GetProductByIdAsync(It.IsAny<string>())).ReturnsAsync(product);
        _wishlistRepositoryMock.Setup(r => r.UpdateProductAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

        // Act
        await _wishlistService.UpdateProductAsync(msg);

        // Assert
        _wishlistRepositoryMock.Verify(r => r.UpdateProductAsync(It.IsAny<Product>()), Times.Once);
    }

    [Test]
    public void UpdateProductAsync_ProductExists_PriceIsNegative_DoesNotUpdateProduct()
    {
        // Arrange
        var msg = new ProductEventDto { ProductId = "product123", Price = -10 };
        var product = new Product { Id = "product123" };
        _wishlistRepositoryMock.Setup(r => r.GetProductByIdAsync(It.IsAny<string>())).ReturnsAsync(product);

        Assert.DoesNotThrowAsync(() => _wishlistService.UpdateProductAsync(msg));
        _wishlistRepositoryMock.Verify(r => r.UpdateProductAsync(It.IsAny<Product>()), Times.Never);
    }

    [Test]
    public async Task UpdateProductAsync_ProductExists_IsAvailableChanges_NotifyCustomers()
    {
        // Arrange
        var msg = new ProductEventDto { ProductId = "product123", IsAvailable = true };
        var product = new Product { Id = "product123", IsAvailable = false };
        _wishlistRepositoryMock.Setup(r => r.GetProductByIdAsync(It.IsAny<string>())).ReturnsAsync(product);
        _wishlistRepositoryMock.Setup(r => r.UpdateProductAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
        _wishlistRepositoryMock.Setup(r => r.GetCustomersWhoHaveWishlistedTheProductAsync(It.IsAny<string>())).ReturnsAsync([""]);
        _eventProducerMock.Setup(e => e.ProduceAsync(It.IsAny<WishlistEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _wishlistService.UpdateProductAsync(msg);

        // Assert
        _wishlistRepositoryMock.Verify(r => r.UpdateProductAsync(It.IsAny<Product>()), Times.Once);
        _eventProducerMock.Verify(e => e.ProduceAsync(It.IsAny<WishlistEventDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateProductAsync_ProductExists_DiscountChanges_NotifyCustomers()
    {
        // Arrange
        var msg = new ProductEventDto { ProductId = "product123", IsDiscounted = true, DiscountedPrice = 10, IsAvailable = true };
        var product = new Product { Id = "product123", IsDiscounted = false, DiscountedPrice = 0, IsAvailable = false };
        _wishlistRepositoryMock.Setup(r => r.GetProductByIdAsync(It.IsAny<string>())).ReturnsAsync(product);
        _wishlistRepositoryMock.Setup(r => r.UpdateProductAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
        _wishlistRepositoryMock.Setup(r => r.GetCustomersWhoHaveWishlistedTheProductAsync(It.IsAny<string>())).ReturnsAsync([""]);
        _eventProducerMock.Setup(e => e.ProduceAsync(It.IsAny<WishlistEventDto>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _wishlistService.UpdateProductAsync(msg);

        // Assert
        _wishlistRepositoryMock.Verify(r => r.UpdateProductAsync(It.IsAny<Product>()), Times.Once);
        _eventProducerMock.Verify(e => e.ProduceAsync(It.IsAny<WishlistEventDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task UpdateProductListingAsync_HappyCase(bool isListed)
    {
        // Arrange
        var msg = new ProductEventDto { ProductId = "product123" };
        _wishlistRepositoryMock.Setup(r => r.UpdateProductListingAsync(It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

        // Act
        await _wishlistService.UpdateProductListingAsync(msg, isListed);

        // Assert
        _wishlistRepositoryMock.Verify(r => r.UpdateProductListingAsync(It.IsAny<string>(), isListed), Times.Once);
    }
    
}
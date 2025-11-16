using System.Net;
using System.Net.Http.Json;
using EcommerceLib.Contract.Events;
using EcommerceLib.Testing.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Wishlist.Contract;

namespace Wishlist.Tests;

[TestFixture]
public class ServiceLevelTests
{
    
    private TestEnvironment<ITestEnvironmentMarker> _testEnvironment;
    private const string CustomerId = "customer1";
    private const string Wishlists = "api/v1/wishlist";
    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _testEnvironment = TestEnvironment<ITestEnvironmentMarker>.Builder
            .WithDatabase<AppDbContext>()
            .WithMockServer()
            .WithConsumer()
            .WithProducer()
            .WithServiceOverride(c =>
            {
                c.Configure<ProductManagementClientConfig>(config =>
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
    public async Task GivenExistingWishlistForCustomer_ShouldReturnWishlist()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var wishlist = CreateWishlist([CreateProduct("prod-1")]);
        db.Wishlists.Add(wishlist);
        await db.SaveChangesAsync();

        // Act
        var response = await client.GetAsync(Wishlists);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Is.Not.Null);
        });
        var data = await response.Content.ReadFromJsonAsync<IEnumerable<WishlistItemDto>?>();
        var wishlistItemDtos = data?.ToList();
        Assert.That(wishlistItemDtos, Is.Not.Null);
        Assert.That(wishlistItemDtos!, Is.Not.Empty);
        Assert.That(wishlistItemDtos![0].Id, Is.EqualTo(wishlist.Products[0].Id));
    }
    
    [Test]
    public async Task GivenNonExistingWishlistForCustomer_ShouldReturnNoContent()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        // Act
        var response = await client.GetAsync(Wishlists);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task GivenNonExistingProductId_AddToWishlist_ShouldReturnNotFound()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();

        var productId = Guid.NewGuid().ToString();
        SetupGetProductById(productId, HttpStatusCode.NotFound);

        // Act
        var response = await client.PostAsync($"{Wishlists}/{productId}", null);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));  
    }
    
    [Test]
    public async Task GivenExistingProductId_AddToWishlist_ShouldAddToWishlist()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();

        var productId = Guid.NewGuid().ToString();
        SetupGetProductById(productId, HttpStatusCode.OK);
        
        // Act
        var response = await client.PostAsync($"{Wishlists}/{productId}", null);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var wishlist = await db.Wishlists
            .Include(w => w.Products)
            .AsNoTracking().FirstOrDefaultAsync(w => w.CustomerId == CustomerId && w.Products.FirstOrDefault(p => p.Id == productId) != null);
        Assert.That(wishlist, Is.Not.Null); 
    }
    
    [Test]
    public async Task GivenNonExistingWishlistForCustomer_RemoveFromWishlist_ShouldReturnNotFound()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        
        // Act
        var response = await client.DeleteAsync($"{Wishlists}/{Guid.NewGuid().ToString()}");

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
    
    [Test]
    public async Task GivenExistingWishlistForCustomer_RemoveFromWishlist_ShouldRemoveItemFromWishlist()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        const string productId = "prod-1";
        db.Wishlists.Add(CreateWishlist([CreateProduct(productId)]));
        await db.SaveChangesAsync();
        
        // Act
        var response = await client.DeleteAsync($"{Wishlists}/{productId}");

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        
        var wishlist = await db.Wishlists
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.CustomerId == CustomerId && w.Products.FirstOrDefault(p => p.Id == productId) != null);
        Assert.That(wishlist, Is.Null); 
    }
    
    [Test]
    public async Task MarkProductAsDelisted_MarksProductAsDelisted()
    {
        // Arrange
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        const string productId = "prod-1";
        db.Products.Add(CreateProduct(productId));
        await db.SaveChangesAsync();
        
        // Act
        await _testEnvironment.EventProducer!.ProduceAsync(Topics.ProductEvents, CreateProductDelistedEvent(productId), Guid.NewGuid().ToString());
        await WaitForConsumerAsync();
        
        // Arrange
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
        Assert.That(product, Is.Not.Null);
        Assert.That(product.IsListed, Is.False);
    }
    
    [Test]
    public async Task UpdateProduct_ProductIsDiscounted_ShouldUpdateProductAndNotifyCustomersWhoWishListedProduct()
    {
        // Arrange
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        const string productId = "prod-1";
        var wishlist = CreateWishlist([CreateProduct(productId)]);
        db.Wishlists.Add(wishlist);
        await db.SaveChangesAsync();
        
        var discountedPrice = wishlist.Products[0].Price - 10;
        var msg = new
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = "ProductListingUpdated",
            CorrelationId = Guid.NewGuid().ToString(),
            ProductId = productId,
            ProductName = "Wireless Noise-Cancelling Headphones",
            Price = 299.99m,
            DiscountedPrice = discountedPrice,
            IsDiscounted = true,
            PrimaryImageUrl = "https://cdn.example.com/products/p-12345/main.jpg",
            IsAvailable = true,
        };
        
        // Act
        await _testEnvironment.EventProducer!.ProduceAsync(Topics.ProductEvents, msg, Guid.NewGuid().ToString());
        await WaitForConsumerAsync();
        
        var updatedProduct = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
        Assert.That(updatedProduct, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(updatedProduct.IsDiscounted, Is.True);
            Assert.That(updatedProduct.DiscountedPrice, Is.EqualTo(discountedPrice));
        });
        
        _testEnvironment.EventConsumer!.ConsumeWithTimeout<WishlistEventDto>(Topics.WishlistEvents, TimeSpan.FromSeconds(3));
        Assert.That(_testEnvironment.EventConsumer!.HasMessageContaining<WishlistEventDto>(m => m.CustomerIds.Contains(CustomerId)), Is.True);
    }
    
    [Test]
    public async Task UpdateProduct_ProductIsAvailable_ShouldUpdateProductAndNotifyCustomersWhoWishListedProduct()
    {
        // Arrange
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        const string productId = "prod-1";
        var product = CreateProduct(productId);
        product.IsAvailable = false;
        var wishlist = CreateWishlist([product]);
        db.Wishlists.Add(wishlist);
        await db.SaveChangesAsync();
        
        var discountedPrice = wishlist.Products[0].Price - 10;
        var msg = new
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = "ProductListingUpdated",
            CorrelationId = Guid.NewGuid().ToString(),
            ProductId = productId,
            ProductName = "Wireless Noise-Cancelling Headphones",
            Price = 299.99m,
            DiscountedPrice = discountedPrice,
            PrimaryImageUrl = "https://cdn.example.com/products/p-12345/main.jpg",
            IsAvailable = true,
            IsListed = true
        };
        
        // Act
        await _testEnvironment.EventProducer!.ProduceAsync(Topics.ProductEvents, msg, Guid.NewGuid().ToString());
        await WaitForConsumerAsync();
        
        var updatedProduct = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
        Assert.That(updatedProduct, Is.Not.Null);
        Assert.That(updatedProduct.IsAvailable, Is.True);
        
        _testEnvironment.EventConsumer!.ConsumeWithTimeout<WishlistEventDto>(Topics.WishlistEvents, TimeSpan.FromSeconds(1));
        Assert.That(_testEnvironment.EventConsumer!.HasMessageContaining<WishlistEventDto>(m => m.CustomerIds.Contains(CustomerId)), Is.True);
    }

    private void SetupGetProductById(string productId, HttpStatusCode expectedStatusCode)
    {
        var response = Response.Create()
            .WithStatusCode(expectedStatusCode);
        if (expectedStatusCode == HttpStatusCode.OK)
        {
            response = response
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { Id = productId, Name = $"prod-{productId}", Price = 5, PrimaryImageUrl = $"example.com/{productId}", IsAvailable = true });
        }
        _testEnvironment.MockServer!
            .Given(Request.Create().WithPath($"/api/v1/products/{productId}")
                .UsingGet())
            .RespondWith(response);
    }
    
    private static async Task WaitForConsumerAsync()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(500));
    }
    
    private static object CreateProductDelistedEvent(string productId)
    {
        return new
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = "ProductDelisted",
            CorrelationId = Guid.NewGuid().ToString(),
            ProductId = productId,
            ProductName = "Wireless Noise-Cancelling Headphones",
            Price = 299.99m,
            PrimaryImageUrl = "https://example.com/image.jpg",
            IsAvailable = false,
            IsListed = false
        };
    }
    
    private static Wishlist CreateWishlist(List<Product> products)
    {
        return new Wishlist
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = CustomerId,
            Products = products
        };
    }

    private static Product CreateProduct(string productId)
    {
        return new Product
        {
            Id = productId,
            Name = "Wireless Noise-Cancelling Headphones",
            Price = 299.99m,
            PrimaryImageUrl = "https://example.com/image.jpg",
            IsListed = true,
            IsAvailable = true,
            IsDiscounted = false,
            AddedAt = DateTime.UtcNow
        };
    } 
}
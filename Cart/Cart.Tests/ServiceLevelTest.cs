using System.Net;
using System.Net.Http.Json;
using Cart.Contract;
using EcommerceLib.Contract.Events;
using EcommerceLib.Testing.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Cart.Tests;

public class ServiceLevelTest
{
    private TestEnvironment<ITestEnvironmentMarker> _testEnvironment;
    private const string Cart = "/api/v1/cart";
    private const string CustomerId = "customer1";
    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _testEnvironment = TestEnvironment<ITestEnvironmentMarker>.Builder
            .WithMockServer()
            .WithConsumer()
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
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ICartStore>();
        db.TryDeleteCart(CustomerId);
    }
    
    [Test]
    public async Task Checkout_HappyCase()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        const string productId = "prod1";
        const int quantity = 2;
        var request = TestDataHelper.CreateCheckoutRequestDto();
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ICartStore>();
        db.TryModifyCart(customerId: CustomerId, [new CartItem(productId, quantity)]);
        SetupGetProductsBatch([productId], [productId]);

        // Act
        var response = await client.PostAsync(Cart + "/checkout", JsonContent.Create(request));
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var checkoutResponseDto = await response.Content.ReadFromJsonAsync<CheckoutResponseDto>();
        checkoutResponseDto.Should().NotBeNull();
        checkoutResponseDto.CartContent.Should().NotBeNull();
        checkoutResponseDto.CartContent.Items.Should().NotBeNull();
        checkoutResponseDto.CartContent.Items.Should().HaveCount(1);
        checkoutResponseDto.CartContent.Items.Should().Contain(i => i.ProductId == productId);
        checkoutResponseDto.CustomerDetails.Should().BeEquivalentTo(request.CustomerDetails);
        checkoutResponseDto.BillingAddress.Should().BeEquivalentTo(request.BillingAddress); 
        checkoutResponseDto.ShippingAddress.Should().BeEquivalentTo(request.ShippingAddress);
        checkoutResponseDto.CheckoutId.Should().NotBeNullOrEmpty();
        checkoutResponseDto.PaymentMethod.Should().Be(request.PaymentMethod);
        checkoutResponseDto.ShippingMethod.Should().Be(request.ShippingMethod);
        checkoutResponseDto.CustomerNotes.Should().Be(request.CustomerNotes);
        _testEnvironment.EventConsumer!.ConsumeWithTimeout<CartCheckedOutEventDto>(Topics.CartEvents, TimeSpan.FromSeconds(1));
        _testEnvironment.EventConsumer!.HasMessageContaining<CartCheckedOutEventDto>(c => c.CustomerId == CustomerId).Should().BeTrue();
    }

    [Test]
    public async Task Checkout_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        
        // Act
        var response = await client.PostAsync(Cart + "/checkout", JsonContent.Create(default(CheckoutRequestDto)));
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Test]
    public async Task Checkout_CartItemCountIsZero_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();

        // Act
        var response = await client.PostAsync(Cart + "/checkout", JsonContent.Create(TestDataHelper.CreateCheckoutRequestDto()));
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Test]
    public async Task GetCart_WhenCartHasItems_ShouldReturnCartItems()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        List<CartItem> cartItems = [
            new("prod1", 1),
            new("prod2", 3),
        ];
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ICartStore>();
        db.TryModifyCart(CustomerId, cartItems);
        var productIds = cartItems.Select(c => c.ProductId).ToList();
        SetupGetProductsBatch(productIds, productIds);

        // Act
        var response = await client.GetAsync(Cart);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartResponse = await response.Content.ReadFromJsonAsync<CartResponse>();
        cartResponse.Should().NotBeNull();
        cartResponse.Items.Should().HaveCount(cartItems.Count);
        cartResponse.Items.Select(i => i.ProductId).Should().BeEquivalentTo(productIds);
        cartResponse.Items.Should().AllSatisfy(i =>
        {
            i.ProductName.Should().NotBeNullOrEmpty();
            i.UnitPrice.Should().BeGreaterThan(0);
            i.Condition.Should().NotBeNullOrEmpty();
            i.ImageUrl.Should().NotBeNullOrEmpty();
        });
    }
    
    [Test]
    public async Task GetCart_NotAllProductsAreListed_ShouldOnlyReturnListedProducts()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        var listedProductIds = new List<string>{"prod2"};
        List<CartItem> cartItems = [
            new("prod1", 1),
            new("prod2", 3),
        ];
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ICartStore>();
        db.TryModifyCart(CustomerId, cartItems);
        var productIds = cartItems.Select(c => c.ProductId).ToList();
        SetupGetProductsBatch(productIds, listedProductIds);

        // Act
        var response = await client.GetAsync(Cart);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartResponse = await response.Content.ReadFromJsonAsync<CartResponse>();
        cartResponse.Should().NotBeNull();
        cartResponse.Items.Should().HaveCount(listedProductIds.Count);
        cartResponse.Items.Select(i => i.ProductId).Should().BeEquivalentTo(listedProductIds);
        cartResponse.Items.Should().AllSatisfy(i =>
        {
            i.ProductName.Should().NotBeNullOrEmpty();
            i.UnitPrice.Should().BeGreaterThan(0);
            i.Condition.Should().NotBeNullOrEmpty();
            i.ImageUrl.Should().NotBeNullOrEmpty();
        });
    }

    [Test]
    public async Task GetCart_WhenCartIsEmpty_ShouldReturnEmptyCart()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();

        // Act
        var response = await client.GetAsync(Cart);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartResponse = await response.Content.ReadFromJsonAsync<CartResponse>();
        cartResponse.Should().NotBeNull();
        cartResponse.Items.Should().BeEmpty();
    }

    [Test]
    public async Task ModifyCart_CartExists_ShouldUpdateCart()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        var initialItems = new List<CartItem>
        {
            new("prod1", 2),
            new("prod2", 3),
            new("prod3", 1)
        };
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ICartStore>();
        db.TryModifyCart(CustomerId, initialItems);
        var updatedCartItems = new List<CartItem>
        {
            new("prod1", 1),
            new("prod2", 5),
            new("prod3", 0)
        };

        // Act
        var response = await client.PutAsync(Cart, JsonContent.Create(updatedCartItems));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var cart = db.GetCartForCustomer(CustomerId);
        var cartItems = cart.ToList();
        cartItems.Should().HaveCount(2);
        cartItems.Should().NotContain(i => i.ProductId == "prod3");
        cartItems.Should().ContainSingle(i => i.ProductId == "prod1" && i.Quantity == 1);        
        cartItems.Should().ContainSingle(i => i.ProductId == "prod2" && i.Quantity == 5);        
    }
    
    [Test]
    public async Task ModifyCart_WhenCartDoesNotExist_ShouldCreateCart()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();

        const string productId = "prod-1";
        const int quantity = 2;
        var cartItems = new List<CartItem>
        {
            new(productId, quantity)
        };

        // Act
        var response = await client.PutAsync(Cart, JsonContent.Create(cartItems));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ICartStore>();
        var cart = db.GetCartForCustomer(CustomerId);
        var enumerable = cart.ToList();
        enumerable.Should().HaveCount(1);
        enumerable.First().ProductId.Should().Be(productId);
    }

    [Test]
    public async Task ModifyCart_WithInvalidQuantity_ShouldReturnBadRequest()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();

        var invalidProductId = "prod-1";
        var invalidItems = new List<CartItem>
        {
            new(invalidProductId, -5)
        };

        // Act
        var response = await client.PutAsync(Cart, JsonContent.Create(invalidItems));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ClearCart_WhenCartHasItems_ShouldRemoveAllItems()
    {
        // Arrange
        const string product1 = "prod-1";
        const string product2 = "prod-2";
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ICartStore>();
        db.TryModifyCart(CustomerId, [
            new CartItem(product1, 1),
            new CartItem(product2, 3)
        ]);
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();

        // Act
        var response = await client.DeleteAsync(Cart);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        db.GetCartForCustomer(CustomerId).Should().BeEmpty();
    }
    
    private void SetupGetProductsBatch(List<string> requestedProductIds, List<string> listedProductIds)
    {
        var products = listedProductIds.Select(id => new
        {
            Id = id,
            Name = "Sample Product",
            Price = 9.99m,
            Condition = "New",
            PrimaryImageUrl = "https://example.com/image.jpg",
            IsDiscounted = false,
            DiscountedPrice = (decimal?)null,
            IsAvailable = true
        }).ToList();

        var responseProducts = products
            .Where(p => requestedProductIds.Contains(p.Id))
            .ToList();

        _testEnvironment.MockServer!
            .Given(Request.Create()
                .WithPath("/api/v1/products/batch/" + string.Join(",", requestedProductIds))
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(responseProducts));
    }

}
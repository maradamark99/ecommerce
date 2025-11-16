using System.Net;
using System.Net.Http.Json;
using EcommerceLib.Auth;
using EcommerceLib.Contract.Events;
using EcommerceLib.Testing.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Review.Contract;
using Review.Model;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Review.Tests;

public class ServiceLevelTests
{
    private TestEnvironment<ITestEnvironmentMarker> _testEnvironment;
    private const string Reviews = "/api/v1/reviews";
    private const string CustomerId = "customer1";
    private const string ProductId = "product1";
    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _testEnvironment = TestEnvironment<ITestEnvironmentMarker>.Builder
            .WithDatabase<AppDbContext>()
            .WithMockServer()
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
    public async Task GivenValidReview_WhenProductIsListedAndCustomerHasPurchasedProduct_CreateShouldReturnCreated()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        SetupGetListedProductById(ProductId, HttpStatusCode.OK);
        await _testEnvironment.EventProducer!.ProduceAsync(Topics.OrderEvents, CreateOrderCompletedEvent(CustomerId, ProductId), Guid.NewGuid().ToString());
        await WaitForConsumerAsync();
        
        // Act
        var response = await client.PostAsync(Reviews, JsonContent.Create(SampleReviewRequest(ProductId)));
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.CustomerPurchases.Any(x => x.CustomerId == CustomerId && x.ProductId == ProductId)
            .Should()
            .BeTrue();
        db.Reviews.Where(x => x.CustomerId == CustomerId && x.ProductId == ProductId)
            .Should()
            .HaveCount(1);
    }
    
    [Test]
    public async Task GivenInValidReview_CreateShouldReturnBadRequest() 
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        
        // Act
        var response = await client.PostAsync(Reviews, JsonContent.Create(new
        {
            ProductId = ProductId,
            Rating = 6.25,
            Comment = ""
            
        }));
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GivenValidReview_WhenProductIsNotListed_CreateShouldReturnNotFound()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();   
        SetupGetListedProductById(ProductId, HttpStatusCode.NotFound);
        
        // Act
        var response = await client.PostAsync(Reviews, JsonContent.Create(SampleReviewRequest(ProductId)));
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Test]
    public async Task GivenValidReview_WhenProductIsListedAndCustomerHasNotPurchasedProduct_CreateShouldReturnUnauthorized() 
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        SetupGetListedProductById(ProductId, HttpStatusCode.OK);
        
        // Act
        var response = await client.PostAsync(Reviews, JsonContent.Create(SampleReviewRequest(ProductId)));
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GivenExistingProductId_WhenProductIsListedAndReviewsExist_GetReviewsForProductShouldReturnReviews()
    {
        // Arrange
        using var client = _testEnvironment.CreateClient();
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = new Product()
        {
            Id = ProductId,
            Condition = "NEW",
            IsListed = true
        };
        var customer1 = new Customer()
        {
            Id = CustomerId,
            FullName = "John Doe"
        };
        var customer2 = new Customer()
        {
            Id = "Other customerId",
            FullName = "Jane Doe"
        };  
        List<Model.Review> reviews =
        [
            new()
            {
                Comment = "Great",
                Rating = 5,
                CreatedAt = DateTime.UtcNow,
                Customer = customer1,
                Product = product
            },
            new()
            {
                Comment = "Good",
                Rating = 4,
                CreatedAt = DateTime.UtcNow,
                Customer = customer2,
                Product = product
            }
        ];
        await db.Reviews.AddRangeAsync(reviews);
        await db.SaveChangesAsync();
        
        // Act
        var response = await client.GetAsync($"{Reviews}/{ProductId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        var reviewsResponse = await response.Content.ReadFromJsonAsync<List<ReviewResponseDto>>();
        reviewsResponse.Should().NotBeNull();
        reviewsResponse.Should().HaveCount(2);
        reviewsResponse.Should().Contain(x => x.Comment == "Great" && x.Rating == 5);
        reviewsResponse.Should().Contain(x => x.Comment == "Good" && x.Rating == 4);
    }

    [Test]
    public async Task GivenExistingProductId_WhenProductIsNotListed_GetReviewsForProductShouldReturnNotFound()
    {
        // Arrange
        using var client = _testEnvironment.CreateClient();
        
        // Act
        var response = await client.GetAsync($"{Reviews}/{ProductId}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GivenExistingProductId_WhenProductIsListedButReviewsDoNotExist_GetReviewsForProductShouldNotReturnReviews()
    {
        // Arrange
        using var client = _testEnvironment.CreateClient();
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = new Product()
        {
            Id = ProductId,
            Condition = "NEW",
            IsListed = true
        };
        await db.Products.AddAsync(product);
        await db.SaveChangesAsync();
        
        // Act
        var response = await client.GetAsync($"{Reviews}/{ProductId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        var reviewsResponse = await response.Content.ReadFromJsonAsync<List<ReviewResponseDto>>();  
        reviewsResponse.Should().NotBeNull();
        reviewsResponse.Should().BeEmpty();
    }

    [Test]
    public async Task GivenExistingReviewId_CustomerIsOwner_DeleteReviewShouldDeleteReview()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = new Product()
        {
            Id = ProductId,
            Condition = "NEW",
            IsListed = true
        };
        var customer = new Customer()
        {
            Id = CustomerId,
            FullName = "John Doe"
        };
        var review = new Model.Review()
        {
            Comment = "Great",
            Rating = 5,
            CreatedAt = DateTime.UtcNow,
            Customer = customer,
            Product = product
        };
        await db.Reviews.AddAsync(review);
        await db.SaveChangesAsync();
        
        // Act
        var response = await client.DeleteAsync($"{Reviews}/{review.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        db.Reviews.Should().BeEmpty();
    }

    [Test]
    public async Task GivenExistingReviewId_CustomerIsNotOwner_DeleteReviewShouldNotDeleteReview()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = new Product()
        {
            Id = ProductId,
            Condition = "NEW",
            IsListed = true
        };
        var customer = new Customer()
        {
            Id = "Other customerId",
            FullName = "John Doe"
        };
        var review = new Model.Review()
        {
            Comment = "Great",
            Rating = 5,
            CreatedAt = DateTime.UtcNow,
            Customer = customer,
            Product = product
        };
        await db.Reviews.AddAsync(review);
        await db.SaveChangesAsync();
        
        // Act
        var response = await client.DeleteAsync($"{Reviews}/{review.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        db.Reviews.Should().NotBeEmpty();
    }

    [Test]
    public async Task GivenExistingReviewId_CustomerIsAdmin_DeleteReviewShouldDeleteReview()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId("Other customerId")
            .WithRoles(Roles.Admin)
            .Build();
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = new Product()
        {
            Id = ProductId,
            Condition = "NEW",
            IsListed = true
        };
        var customer = new Customer()
        {
            Id = CustomerId,
            FullName = "John Doe"
        };
        var review = new Model.Review()
        {
            Comment = "Great",
            Rating = 5,
            CreatedAt = DateTime.UtcNow,
            Customer = customer,
            Product = product
        };
        await db.Reviews.AddAsync(review);
        await db.SaveChangesAsync();
        
        // Act
        var response = await client.DeleteAsync($"{Reviews}/{review.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        db.Reviews.Should().BeEmpty();
    }

    [Test]
    public async Task GivenNotExistingReviewId_DeleteReviewShouldReturnNotFound()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        
        // Act
        var response = await client.DeleteAsync($"{Reviews}/69");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Test]
    public async Task GivenValidReviewAndExistingReviewId_UpdateReviewShouldUpdateReview()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .WithRoles(Roles.Customer)
            .Build();
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = new Product()
        {
            Id = ProductId,
            Condition = "NEW",
            IsListed = true
        };
        var customer = new Customer()
        {
            Id = CustomerId,
            FullName = "John Doe"
        };
        var review = new Model.Review()
        {
            Comment = "Bad",
            Rating = 2,
            CreatedAt = DateTime.UtcNow,
            Customer = customer,
            Product = product
        };
        var newRating = 5;
        var newComment = "Good";
        var updateWith = new
        {
            ProductId = ProductId,
            Rating = newRating,
            Comment = newComment
        };
        await db.Reviews.AddAsync(review);
        await db.SaveChangesAsync();
        
        // Act
        var response = await client.PutAsync($"{Reviews}/{review.Id}", JsonContent.Create(updateWith));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var updatedReview = await db.Reviews.AsNoTracking().FirstAsync(r => r.Id == review.Id);
        updatedReview.Comment.Should().Be(newComment);
        updatedReview.Rating.Should().Be(newRating);
    }

    [Test]
    public async Task GivenNotValidReview_UpdateReviewShouldReturnBadRequest()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        
        // Act
        var response = await client.PutAsync($"{Reviews}/69", JsonContent.Create(new
        {
            ProductId = ProductId,
            Rating = 6.25,
            Comment = ""
        }));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Test]
    public async Task GivenNotExistingReviewId_UpdateReviewShouldReturnNotFound()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .Build();
        
        // Act
        var response = await client.PutAsync($"{Reviews}/69", JsonContent.Create(SampleReviewRequest(ProductId)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Test]
    public async Task GivenValidReviewAndExistingReviewId_CustomerIsNotOwner_UpdateReviewShouldReturnUnauthorized()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId("Other customerId")
            .WithRoles(Roles.Customer)
            .Build();
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = new Product()
        {
            Id = ProductId,
            Condition = "NEW",
            IsListed = true
        };
        var customer = new Customer()
        {
            Id = CustomerId,
            FullName = "John Doe"
        };
        var review = new Model.Review()
        {
            Comment = "Great",
            Rating = 5,
            CreatedAt = DateTime.UtcNow,
            Customer = customer,
            Product = product
        };
        await db.Reviews.AddAsync(review);
        await db.SaveChangesAsync();
        
        // Act
        var response = await client.PutAsync($"{Reviews}/{review.Id}", JsonContent.Create(SampleReviewRequest(ProductId)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Test]
    public async Task GivenValidReviewAndExistingReviewId_ChangesProductId_UpdateReviewShouldReturnBadRequest()
    {
        // Arrange
        using var client = _testEnvironment.CreateAuthorizedClientBuilder()
            .WithUserId(CustomerId)
            .WithRoles(Roles.Customer)
            .Build();
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = new Product()
        {
            Id = "other-productId",
            Condition = "NEW",
            IsListed = true
        };
        var customer = new Customer()
        {
            Id = CustomerId,
            FullName = "John Doe"
        };
        var review = new Model.Review()
        {
            Comment = "Great",
            Rating = 5,
            CreatedAt = DateTime.UtcNow,
            Customer = customer,
            Product = product
        };
        await db.Reviews.AddAsync(review);
        await db.SaveChangesAsync();
        
        // Act
        var response = await client.PutAsync($"{Reviews}/{review.Id}", JsonContent.Create(SampleReviewRequest(ProductId)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Test]
    public async Task GivenExistingListedProduct_MarkProductAsDelistedShouldDelistProduct()
    {
        // Arrange
        var productDelistedEvent = new ProductEventDto
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.ProductDelisted),
            CorrelationId = Guid.NewGuid().ToString(),
            ProductId = ProductId,
            ProductName = "Gaming Laptop",
        };
        await using var scope = _testEnvironment.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = new Product()
        {
            Id = ProductId,
            Condition = "NEW",
            IsListed = true
        };
        await db.Products.AddAsync(product);
        await db.SaveChangesAsync();
        
        // Act
        await _testEnvironment.EventProducer!.ProduceAsync(Topics.ProductEvents, productDelistedEvent, Guid.NewGuid().ToString());
        await WaitForConsumerAsync();
        
        // Assert
        var updatedProduct = await db.Products.AsNoTracking().FirstAsync(p => p.Id == ProductId);
        updatedProduct.Should().NotBeNull();
        updatedProduct.IsListed.Should().BeFalse();
    }
    
    private void SetupGetListedProductById(string productId, HttpStatusCode statusCode)
    {
        var response = Response.Create()
            .WithStatusCode(statusCode);
        if (statusCode == HttpStatusCode.OK)
        {
            response = response
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { id = productId, condition = "NEW"});
        }
        _testEnvironment.MockServer!
            .Given(Request.Create().WithPath($"/api/v1/products/{productId}")
                .UsingGet())
            .RespondWith(response);
    }
    
    private static object SampleReviewRequest(string productId) => new
    {
        ProductId = productId,
        Rating = 5,
        Comment = "Great product",
    };
    
    private static object CreateOrderCompletedEvent(string customerId, string productId)
    {
        return new
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.OrderCompleted),
            CorrelationId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Order = new
            {
                OrderId = "ORD-123456",
                Customer = new
                {
                    CustomerId = customerId,
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
                        ProductId = productId,
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
                PaymentMethod = "CreditCard"
            }
        };
    }
    
    private static async Task WaitForConsumerAsync()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(200));
    }
}
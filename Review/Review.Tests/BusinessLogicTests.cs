using EcommerceLib.Auth;
using EcommerceLib.Contract.Dto;
using EcommerceLib.Contract.Events;
using EcommerceLib.Exception;
using Moq;
using Review.Contract;
using Review.Model;

namespace Review.Tests;

public class BusinessLogicTests
{
    private const string TestCustomerId = "customer1";
    private const string TestProductId = "product1";
    private ReviewService _reviewService;
    private Mock<IReviewRepository> _reviewRepositoryMock;
    private Mock<IProductManagementClient> _productManagementClientMock;
    
    [SetUp] 
    public void Setup()
    {
        _reviewRepositoryMock = new Mock<IReviewRepository>();   
        _productManagementClientMock = new Mock<IProductManagementClient>();
        _reviewService = new ReviewService(_reviewRepositoryMock.Object, _productManagementClientMock.Object);
        
        _reviewRepositoryMock.Setup(r => r.IsProductListedAsync(It.IsAny<string>())).ReturnsAsync(true);
        _reviewRepositoryMock.Setup(r => r.HasCustomerPurchasedProduct(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _productManagementClientMock.Setup(r => r.GetListedProductById(It.IsAny<string>()))
            .ReturnsAsync(new ProductDto(It.IsAny<string>(), It.IsAny<string>()));
    }
    
    [Test]
    public async Task GetReviewsForProductAsync_ProductListed_ReturnsReviews()
    {
        // Arrange
        var productId = "existing-product";
        var reviews = new List<Model.Review>
        {
            new() { Id = 1, ProductId = productId, Rating = 5, Comment = "Great product!" },
            new() { Id = 2, ProductId = productId, Rating = 4, Comment = "Good value." }
        };
        _reviewRepositoryMock.Setup(r => r.GetReviewsForProductAsync(productId)).ReturnsAsync(reviews);
        
        // Act
        var result = await _reviewService.GetReviewsForProductAsync(productId);
        
        // Assert
        Assert.That(result, Is.EqualTo(reviews));
    }  
    
    [Test]
    public void GetReviewsForProductAsync_ProductNotListed_ThrowsNotFoundException()
    {
        // Arrange
        var productId = "nonexistent-product";
        _reviewRepositoryMock.Setup(r => r.IsProductListedAsync(productId)).ReturnsAsync(false);
        
        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(async () =>
            await _reviewService.GetReviewsForProductAsync(productId));
    }
    
    [Test]
    public void CreateReviewAsync_ProductListedAndCustomerPurchased_CreatesReview()
    {
        // Arrange
        var request = CreateReviewRequestDto();
        var customerId = TestCustomerId;
        const int reviewId = 1;
        
        _reviewRepositoryMock.Setup(r => r.CreateReviewAsync(It.IsAny<Model.Review>()))
            .ReturnsAsync(reviewId);
        
        // Act
        var result = _reviewService.CreateReviewAsync(customerId, request).Result;
        
        // Assert
        Assert.That(result, Is.EqualTo(reviewId));
        _reviewRepositoryMock.Verify(r => r.CreateReviewAsync(It.Is<Model.Review>(rev =>
            rev.CustomerId == customerId &&
            rev.ProductId == request.ProductId &&
            rev.Rating == request.Rating &&
            rev.Comment == request.Comment
        )), Times.Once);
    }
    
    [Test]
    public void CreateReviewAsync_ProductNotListed_ThrowsNotFoundException()
    {
        // Arrange
        var request = CreateReviewRequestDto();
        var customerId = TestCustomerId;
        _productManagementClientMock.Setup(r => r.GetListedProductById(request.ProductId))
            .ReturnsAsync(default(ProductDto));
        
        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(async () =>
            await _reviewService.CreateReviewAsync(customerId, request));
    }   
    
    [Test]
    public void CreateReviewAsync_CustomerNotPurchasedProduct_ThrowsUnauthorizedException()
    {
        // Arrange
        var request = CreateReviewRequestDto();
        var customerId = TestCustomerId;
        
        _reviewRepositoryMock.Setup(r => r.HasCustomerPurchasedProduct(customerId, request.ProductId))
            .ReturnsAsync(false);
        
        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedException>(async () =>
            await _reviewService.CreateReviewAsync(customerId, request));
    }
    
    [Test]
    public void DeleteReviewAsync_ReviewExistsAndOwnedByUser_DeletesReview()
    {
        // Arrange
        const int reviewId = 1;
        var user = new AppUser()
        {
            Id = TestCustomerId,
            Roles = [nameof(Roles.Customer)]
        };
        var existingReview = new Model.Review
        {
            Id = reviewId,
            Customer = new Model.Customer { Id = user.Id },
            ProductId = TestProductId,
            Rating = 5,
            Comment = "Great product!"
        };
        
        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(reviewId)).ReturnsAsync(existingReview);
        
        Assert.DoesNotThrowAsync(async () => await _reviewService.DeleteReviewAsync(reviewId, user));
        
        // Assert
        _reviewRepositoryMock.Verify(r => r.DeleteReviewAsync(reviewId), Times.Once);
    }
    
    [Test]
    public void DeleteReviewAsync_ReviewDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        const int reviewId = 1;
        const string userId = TestCustomerId;
        var user = new AppUser()
        {
            Id = TestCustomerId,
            Roles = [nameof(Roles.Customer)]
        };
        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(reviewId)).ReturnsAsync(default(Model.Review));
        
        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(async () =>
            await _reviewService.DeleteReviewAsync(reviewId, user));
    }
    
    [Test]
    public void DeleteReviewAsync_ReviewOwnedByDifferentUser_ThrowsUnauthorizedException()
    {
        // Arrange
        const int reviewId = 1;
        var user = new AppUser()
        {
            Id = TestCustomerId,
            Roles = [nameof(Roles.Customer)]
        };
        var existingReview = new Model.Review
        {
            Id = reviewId,
            Customer = new Model.Customer { Id = "different-customer" },
            ProductId = "product1",
            Rating = 5,
            Comment = "Great product!"
        };
        
        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(reviewId)).ReturnsAsync(existingReview);
        
        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedException>(async () =>
            await _reviewService.DeleteReviewAsync(reviewId, user));
    }
    
    [Test]  
    public void UpdateReviewAsync_ReviewExistsAndOwnedByUserProductIdNotChanged_UpdatesReview()
    {
        // Arrange
        const int reviewId = 1;
        const string userId = TestCustomerId;
        var existingReview = new Model.Review
        {
            Id = reviewId,
            Customer = new Model.Customer { Id = userId },
            ProductId = TestProductId,
            Rating = 4,
            Comment = "Good product."
        };
        var updateRequest = CreateReviewRequestDto();
        
        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(reviewId)).ReturnsAsync(existingReview);
        
        // Act
        Assert.DoesNotThrowAsync(async () => 
            await _reviewService.UpdateReviewAsync(reviewId, userId, updateRequest));
        
        // Assert
        _reviewRepositoryMock.Verify(r => r.UpdateReviewAsync(It.Is<Model.Review>(rev =>
            rev.Id == reviewId &&
            rev.Customer.Id == userId &&
            rev.ProductId == updateRequest.ProductId &&
            rev.Rating == updateRequest.Rating &&
            rev.Comment == updateRequest.Comment
        )), Times.Once);
    }
    
    [Test]  
    public void UpdateReviewAsync_ReviewDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        const int reviewId = 1;
        const string userId = TestCustomerId;
        var updateRequest = CreateReviewRequestDto();
        
        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(reviewId)).ReturnsAsync(default(Model.Review));
        
        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(async () =>
            await _reviewService.UpdateReviewAsync(reviewId, userId, updateRequest));
    }
    
    [Test]
    public void UpdateReviewAsync_ReviewOwnedByDifferentUser_ThrowsUnauthorizedException()
    {
        // Arrange
        const int reviewId = 1;
        const string userId = TestCustomerId;
        var existingReview = new Model.Review
        {
            Id = reviewId,
            Customer = new Model.Customer { Id = "different-customer" },
            ProductId = "product1",
            Rating = 4,
            Comment = "Good product."
        };
        var updateRequest = CreateReviewRequestDto();
        
        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(reviewId)).ReturnsAsync(existingReview);
        
        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedException>(async () =>
            await _reviewService.UpdateReviewAsync(reviewId, userId, updateRequest));
    }
    
    [Test]
    public void UpdateReviewAsync_ProductIdChanged_ThrowsBadRequestException()
    {
        // Arrange
        const int reviewId = 1;
        const string userId = TestCustomerId;
        var existingReview = new Model.Review
        {
            Id = reviewId,
            Customer = new Model.Customer { Id = userId },
            ProductId = TestProductId,
            Rating = 4,
            Comment = "Good product."
        };
        var updateRequest = new ReviewRequestDto(ProductId: "different-product", Rating: 5, Comment: "Excellent!");
        
        _reviewRepositoryMock.Setup(r => r.GetByIdAsync(reviewId)).ReturnsAsync(existingReview);
        
        // Act & Assert
        Assert.ThrowsAsync<BadRequestException>(async () =>
            await _reviewService.UpdateReviewAsync(reviewId, userId, updateRequest));
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public void UpdateProductListingAsync_HappyCase(bool isListed)
    {
        // Arrange
        var productEvent = new ProductEventDto
        {
            ProductId = TestProductId,
        };
        
        _reviewRepositoryMock.Setup(r => r.UpdateProductListingAsync(productEvent.ProductId, isListed))
            .Returns(Task.CompletedTask);
        
        // Act
        Assert.DoesNotThrowAsync(async () => 
            await _reviewService.UpdateProductListingAsync(productEvent, isListed));
        
        // Assert
        _reviewRepositoryMock.Verify(r => r.UpdateProductListingAsync(productEvent.ProductId, isListed), Times.Once);
        
    }
    
    [Test]
    public async Task SaveCustomerPurchasesAsync_AllNew_InsertsPurchases()
    {
        // Arrange
        var orderEvent = CreateOrderEventDto();
        _reviewRepositoryMock.Setup(r => r.GetCustomerPurchasesAsync(It.IsAny<string>(), It.IsAny<List<string>>()))
            .ReturnsAsync(new List<CustomerPurchase>());
        _reviewRepositoryMock.Setup(r => r.SaveCustomerPurchasesAsync(It.IsAny<List<CustomerPurchase>>()))
            .Returns(Task.CompletedTask);

        // Act
        await _reviewService.SaveCustomerPurchasesAsync(orderEvent);

        // Assert
        _reviewRepositoryMock.Verify(r => r.SaveCustomerPurchasesAsync(It.Is<List<CustomerPurchase>>(l => l.Count == 2)), Times.Once);
        _reviewRepositoryMock.Verify(r => r.UpdateCustomerPurchasesAsync(It.IsAny<List<CustomerPurchase>>()), Times.Never);
    }

    [Test]
    public async Task SaveCustomerPurchasesAsync_AllExisting_UpdatesPurchases()
    {
        // Arrange
        var orderEvent = CreateOrderEventDto();
        var existingPurchases = orderEvent.Order.Items.Select(i => new CustomerPurchase { ProductId = i.ProductId }).ToList();
        _reviewRepositoryMock.Setup(r => r.GetCustomerPurchasesAsync(It.IsAny<string>(), It.IsAny<List<string>>()))
            .ReturnsAsync(existingPurchases);
        _reviewRepositoryMock.Setup(r => r.UpdateCustomerPurchasesAsync(It.IsAny<List<CustomerPurchase>>()))
            .Returns(Task.CompletedTask);

        // Act
        await _reviewService.SaveCustomerPurchasesAsync(orderEvent);

        // Assert
        _reviewRepositoryMock.Verify(r => r.UpdateCustomerPurchasesAsync(It.Is<List<CustomerPurchase>>(l => l.Count == 2)), Times.Once);
        _reviewRepositoryMock.Verify(r => r.SaveCustomerPurchasesAsync(It.IsAny<List<CustomerPurchase>>()), Times.Never);
    }

    [Test]
    public async Task SaveCustomerPurchasesAsync_Mixed_UpdatesAndInserts()
    {
        // Arrange
        var orderEvent = CreateOrderEventDto();
        var existingPurchases = new List<CustomerPurchase> { new CustomerPurchase { ProductId = "product1" } };
        _reviewRepositoryMock.Setup(r => r.GetCustomerPurchasesAsync(It.IsAny<string>(), It.IsAny<List<string>>()))
            .ReturnsAsync(existingPurchases);
        _reviewRepositoryMock.Setup(r => r.UpdateCustomerPurchasesAsync(It.IsAny<List<CustomerPurchase>>()))
            .Returns(Task.CompletedTask);
        _reviewRepositoryMock.Setup(r => r.SaveCustomerPurchasesAsync(It.IsAny<List<CustomerPurchase>>()))
            .Returns(Task.CompletedTask);

        // Act
        await _reviewService.SaveCustomerPurchasesAsync(orderEvent);

        // Assert
        _reviewRepositoryMock.Verify(r => r.UpdateCustomerPurchasesAsync(It.Is<List<CustomerPurchase>>(l => l.Count == 1)), Times.Once);
        _reviewRepositoryMock.Verify(r => r.SaveCustomerPurchasesAsync(It.Is<List<CustomerPurchase>>(l => l.Count == 1)), Times.Once);
    }
    
    private static OrderEventDto CreateOrderEventDto()
    {
        return new OrderEventDto
        {
            Order = new OrderDto
            {
                Customer = new CustomerDto { CustomerId = "customer1" },
                CreatedAt = DateTime.UtcNow,
                Items = new List<ItemDto>
                {
                    new() { ProductId = "product1" },
                    new() { ProductId = "product2" }
                }
            }
        };
    }
    
    private static ReviewRequestDto CreateReviewRequestDto()
    {
        return new ReviewRequestDto(ProductId: TestProductId, Rating: 5, Comment: "Excellent!");
    }
    
}
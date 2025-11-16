using Ecommerce.Common.Exception;
using Ecommerce.Domain.Auth;
using Ecommerce.Domain.OrderManagement;
using Ecommerce.Domain.OrderManagement.Contract;
using Ecommerce.Domain.ProductManagement.ProductManagement.Contract;
using Ecommerce.Domain.Review.Contract;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Domain.Review;

public class ReviewService(
    IProductManagementService productManagementService,
    IOrderManagementService orderManagementService,
    IReviewRepository reviewRepository,
    UserManager<AppUser> userManager) : IReviewService
{
    
    public async Task<IEnumerable<Review>> GetReviewsForProductAsync(string productId)
    {
        if (!Guid.TryParse(productId, out var productGuid))
        {
            throw new BadRequestException($"Invalid product ID format: {productId}");
        }
        return await reviewRepository.GetReviewsForProductAsync(productGuid);
    }

    public async Task<long> CreateReviewAsync(string customerId, ReviewRequestDto request)
    {
        await EnsureCustomerHasPurchasedTheProductAsync(customerId, request.ProductId);
        var customer = await userManager.FindByIdAsync(customerId);
        var product = await productManagementService.GetProductByIdAsync(request.ProductId);
        if (product == null) 
        {
            throw new NotFoundException($"Product with id: {request.ProductId} not found.");
        }
        if (!Guid.TryParse(request.ProductId, out var productGuid))
        {
            throw new BadRequestException($"Invalid product ID format: {request.ProductId}");
        }
        var review = new Review
        {
            Customer = customer!,
            ProductId = productGuid,    
            Rating = request.Rating,
            Comment = request.Comment,
        };
        return await reviewRepository.CreateReviewAsync(review);
    }

    public async Task DeleteReviewAsync(long id, string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        var roles = await userManager.GetRolesAsync(user!);
        var review = await reviewRepository.GetByIdAsync(id);
        if (review == null)
        {
            throw new NotFoundException($"Review with id: {id} not found.");
        }
        if (!roles.Contains(nameof(Roles.Admin)) && review.Customer.Id != userId) 
        {
            throw new UnauthorizedException("You do not have permission to delete this review.");
        }
        await reviewRepository.DeleteReviewAsync(id);
    }

    public async Task UpdateReviewAsync(long id, string customerId, ReviewRequestDto request)
    {
        var existingReview = await reviewRepository.GetByIdAsync(id);
        if (existingReview == null)
        {
            throw new NotFoundException($"Review with id: {id} not found.");
        }
        if (customerId != existingReview.Customer.Id)
        {
            throw new UnauthorizedException("You do not have permission to update this review.");
        }
        if (request.ProductId != existingReview.Product.Id.ToString()) 
        {
            throw new BadRequestException("You cannot change the product of an existing review.");
        }
        var reviewToUpdate = new Review
        {
            Id = existingReview.Id,
            Customer = existingReview.Customer,
            Product = existingReview.Product,
            Rating = request.Rating,
            Comment = request.Comment
        };
        await reviewRepository.UpdateReviewAsync(reviewToUpdate);
    }
    
    private async Task EnsureCustomerHasPurchasedTheProductAsync(string customerId, string productId)
    {
        var orderHistory = await orderManagementService.GetOrderHistoryAsync(customerId);
        var hasPurchasedProduct = orderHistory.Any(order => 
            order.CurrentStatus == Status.Delivered &&
            order.OrderItems.Any(item => item.ProductId == productId));
        if (!hasPurchasedProduct) 
        {
            throw new UnauthorizedException($"Customer with id: {customerId} has not purchased product with id: {productId}.");
        }
    }
}
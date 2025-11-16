using EcommerceLib.Auth;
using EcommerceLib.Contract.Dto;
using EcommerceLib.Contract.Events;
using EcommerceLib.Exception;
using Review.Contract;
using Review.Model;

namespace Review;

public class ReviewService(
    IReviewRepository reviewRepository,
    IProductManagementClient productManagementClient
) : IReviewService
{
    
    public async Task<IEnumerable<Model.Review>> GetReviewsForProductAsync(string productId)
    {
        if (!await reviewRepository.IsProductListedAsync(productId)) 
        {
            throw new NotFoundException($"Product with id: {productId} not found.");
        }
        return await reviewRepository.GetReviewsForProductAsync(productId);
    }

    public async Task<long> CreateReviewAsync(string customerId, ReviewRequestDto request)
    {
        await EnsureProductIsListedAsync(request.ProductId);
        await EnsureCustomerHasPurchasedTheProductAsync(customerId, request.ProductId);
        var review = new Model.Review
        {
            CustomerId = customerId,
            ProductId = request.ProductId,    
            Rating = request.Rating,
            Comment = request.Comment,
        };
        return await reviewRepository.CreateReviewAsync(review);
    }

    public async Task DeleteReviewAsync(long id, AppUser user)
    {
        var review = await reviewRepository.GetByIdAsync(id);
        if (review == null)
        {
            throw new NotFoundException($"Review with id: {id} not found.");
        }
        if (!user.Roles.Contains(nameof(Roles.Admin)) && review.Customer.Id != user.Id)
        {
            throw new UnauthorizedException("You do not have permission to delete this review.");
        }
        await reviewRepository.DeleteReviewAsync(id);
    }

    public async Task UpdateReviewAsync(long id, string customerId, ReviewRequestDto request)
    {
        var toUpdate = await reviewRepository.GetByIdAsync(id);
        if (toUpdate == null)
        {
            throw new NotFoundException($"Review with id: {id} not found.");
        }
        if (customerId != toUpdate.Customer.Id)
        {
            throw new UnauthorizedException("You do not have permission to update this review.");
        }
        if (request.ProductId != toUpdate.ProductId) 
        {
            throw new BadRequestException("You cannot change the product of an existing review.");
        }
        toUpdate.Comment = request.Comment;
        toUpdate.Rating = request.Rating;
        toUpdate.UpdatedAt = DateTime.UtcNow;
        await reviewRepository.UpdateReviewAsync(toUpdate);
    }

    public async Task SaveCustomerPurchasesAsync(OrderEventDto orderEvent)
    {
        var customerId = orderEvent.Order.Customer.CustomerId;
        var orderDate = orderEvent.Order.CreatedAt;
        var productIds = orderEvent.Order.Items.Select(i => i.ProductId).ToList();

        var existingPurchases = (await reviewRepository.GetCustomerPurchasesAsync(customerId, productIds)).ToList();

        var purchasesToUpdate = new List<CustomerPurchase>();
        var purchasesToInsert = new List<CustomerPurchase>();
        var customer = new Customer() { Id = customerId, FullName = orderEvent.Order.Customer.FullName };

        foreach (var item in orderEvent.Order.Items)
        {
            var existing = existingPurchases.FirstOrDefault(p => p.ProductId == item.ProductId);
            if (existing != null)
            {
                existing.LatestPurchaseDate = orderDate;
                purchasesToUpdate.Add(existing);
            }
            else
            {
                var newPurchase = CreateCustomerPurchase(customer, item);
                newPurchase.LatestPurchaseDate = orderDate;
                purchasesToInsert.Add(newPurchase);
            }
        }

        if (purchasesToUpdate.Count != 0)
            await reviewRepository.UpdateCustomerPurchasesAsync(purchasesToUpdate);

        if (purchasesToInsert.Count != 0)
            await reviewRepository.SaveCustomerPurchasesAsync(purchasesToInsert);
    }


    public async Task UpdateProductListingAsync(ProductEventDto productEvent, bool isListed)
    {
        await reviewRepository.UpdateProductListingAsync(productEvent.ProductId, isListed);
    }

    private async Task EnsureCustomerHasPurchasedTheProductAsync(string customerId, string productId)
    {
        if (!await reviewRepository.HasCustomerPurchasedProduct(customerId, productId)) 
        {
            throw new UnauthorizedException($"Customer with id: {customerId} has not purchased product with id: {productId}.");
        }
    }
    
    private async Task EnsureProductIsListedAsync(string productId)
    {
        var product = await productManagementClient.GetListedProductById(productId);
        if (product == null) 
        {
            throw new NotFoundException($"Product with id: {productId} is not listed.");
        }
    }   
  
    private static CustomerPurchase CreateCustomerPurchase(Customer customer, ItemDto i)
    {
        return new CustomerPurchase()
        {
            Customer = customer,
            Product = new Product()
            {
                Id = i.ProductId,
                Condition = i.Condition,
                IsListed = true
            }
        };
    }
    
}
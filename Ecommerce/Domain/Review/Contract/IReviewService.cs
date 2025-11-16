namespace Ecommerce.Domain.Review.Contract;

public interface IReviewService
{
    public Task<IEnumerable<Review>> GetReviewsForProductAsync(string productId);
    
    public Task<long> CreateReviewAsync(string customerId, ReviewRequestDto request);
    
    public Task DeleteReviewAsync(long id, string userId);
    
    public Task UpdateReviewAsync(long id, string customerId, ReviewRequestDto request);
}
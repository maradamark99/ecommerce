namespace Ecommerce.Domain.Review.Contract;

public interface IReviewRepository
{
    
    Task<Review?> GetByIdAsync(long id);
    
    Task<IEnumerable<Review>> GetReviewsForProductAsync(Guid productId);
    
    Task<long> CreateReviewAsync(Review review);
    
    Task DeleteReviewAsync(long id);
    
    Task UpdateReviewAsync(Review review);
}
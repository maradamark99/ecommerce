using Ecommerce.Common.Data;
using Ecommerce.Domain.Review.Contract;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Domain.Review;

public class ReviewRepository(AppDbContext dbContext) : IReviewRepository
{
    public async Task<Review?> GetByIdAsync(long id)
    {
        return await dbContext.Reviews.FindAsync(id);
    }

    public async Task<IEnumerable<Review>> GetReviewsForProductAsync(Guid productId)
    {
        return await dbContext.Reviews
            .Include(r => r.Product)
            .Where(r => r.Product.Id == productId)
            .ToListAsync();
    }

    public Task<long> CreateReviewAsync(Review review)
    {
        dbContext.Reviews.Add(review);
        return dbContext.SaveChangesAsync().ContinueWith(t => review.Id);
    }

    public Task DeleteReviewAsync(long id)
    {
        return dbContext.Reviews.Where(r => r.Id == id).ExecuteDeleteAsync();
    }

    public Task UpdateReviewAsync(Review review)
    {
        dbContext.Reviews.Update(review);
        return dbContext.SaveChangesAsync();
    }
}
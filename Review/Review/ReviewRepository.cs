using Microsoft.EntityFrameworkCore;
using Review.Contract;
using Review.Model;

namespace Review;

public class ReviewRepository(AppDbContext dbContext) : IReviewRepository
{
    public async Task<Model.Review?> GetByIdAsync(long id)
    {
        return await dbContext.Reviews.
            Include(r => r.Customer).
            Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Model.Review>> GetReviewsForProductAsync(string productId)
    {
        return await dbContext.Reviews
            .Include(r => r.Customer)
            .Include(r => r.Product)
            .Where(r => r.ProductId == productId)
            .ToListAsync();
    }

    public Task<long> CreateReviewAsync(Model.Review review)
    {
        dbContext.Reviews.Add(review);
        return dbContext.SaveChangesAsync().ContinueWith(t => review.Id);
    }

    public Task DeleteReviewAsync(long id)
    {
        return dbContext.Reviews.Where(r => r.Id == id).ExecuteDeleteAsync();
    }

    public Task UpdateReviewAsync(Model.Review review)
    {
        dbContext.Reviews.Update(review);
        return dbContext.SaveChangesAsync();
    }

    public Task<bool> HasCustomerPurchasedProduct(string customerId, string productId)
    {
        return dbContext.CustomerPurchases
            .AnyAsync(c => c.CustomerId == customerId && c.ProductId == productId);
    }

    public Task SaveCustomerPurchasesAsync(List<CustomerPurchase> customerPurchases)
    {
        dbContext.CustomerPurchases.AddRange(customerPurchases);
        return dbContext.SaveChangesAsync();
    }

    public Task<bool> IsProductListedAsync(string productId)
    {
        return dbContext.Products
            .AnyAsync(p => p.Id == productId && p.IsListed);
    }

    public Task UpdateProductListingAsync(string productId, bool isListed)
    {
        return dbContext.Products
            .Where(r => r.Id == productId)
            .ExecuteUpdateAsync(r => r.SetProperty(x => x.IsListed, isListed));
    }

    public Task<List<CustomerPurchase>> GetCustomerPurchasesAsync(string customerId, List<string> productIds)
    {
        return dbContext.CustomerPurchases
            .Where(c => c.CustomerId == customerId && productIds.Contains(c.ProductId))
            .ToListAsync();
    }

    public Task UpdateCustomerPurchasesAsync(List<CustomerPurchase> purchasesToUpdate)
    {
        dbContext.CustomerPurchases.UpdateRange(purchasesToUpdate);
        return dbContext.SaveChangesAsync();
    }
}
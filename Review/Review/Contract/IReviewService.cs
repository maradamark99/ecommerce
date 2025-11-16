using EcommerceLib.Auth;
using EcommerceLib.Contract.Events;

namespace Review.Contract;

public interface IReviewService
{
    public Task<IEnumerable<Model.Review>> GetReviewsForProductAsync(string productId);
    
    public Task<long> CreateReviewAsync(string customerId, ReviewRequestDto request);
    
    public Task DeleteReviewAsync(long id, AppUser user);
    
    public Task UpdateReviewAsync(long id, string customerId, ReviewRequestDto request);
    
    public Task SaveCustomerPurchasesAsync(OrderEventDto orderEvent);
    
    public Task UpdateProductListingAsync(ProductEventDto productEvent, bool isListed);
    
}
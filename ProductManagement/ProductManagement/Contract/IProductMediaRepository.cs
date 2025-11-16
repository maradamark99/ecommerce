using ProductManagement.Model;

namespace ProductManagement.Contract;

public interface IProductMediaRepository
{
    Task<ProductMedia?> GetProductMediaByIdAsync(string productId, long mediaId);
    Task AddMediaAsync(string productId, ProductMedia media);
    
    Task RemoveMediaAsync(string productId, long mediaId);
}
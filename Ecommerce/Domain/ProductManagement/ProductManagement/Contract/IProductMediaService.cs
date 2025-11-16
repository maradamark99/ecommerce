namespace Ecommerce.Domain.ProductManagement.ProductManagement.Contract;

public interface IProductMediaService
{
    Task<ProductMediaResponse> AddMediaAsync(string productId, IFormFile file, bool isPrimaryImage);
    
    Task RemoveMediaAsync(string productId, long mediaId);
}
namespace Wishlist.Contract;

public interface IProductManagementClient
{
    Task<ProductDto?> GetProductByIdAsync(string productId);
}
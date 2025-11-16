namespace Cart.Contract;

public interface IProductManagementClient
{
    Task<List<ProductDto>> GetProductsByIdsAsync(IEnumerable<string> productId);
}
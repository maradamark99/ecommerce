namespace Review.Contract;

public interface IProductManagementClient
{
    Task<ProductDto?> GetListedProductById(string productId);
}
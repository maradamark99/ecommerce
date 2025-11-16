using EcommerceLib.Pagination;

namespace ProductManagement.Contract;

public interface IProductManagementService
{
    
    Task<Paged<ProductManagementResponse>> GetAllProductsAsync(Pager pager, Sorter sorter);
    
    Task<ProductManagementResponse> GetProductByIdAsync(string id);
    
    Task<string> CreateProductAsync(CreateProductRequestDto createProductRequestDto);

    Task DeleteProductByIdAsync(string id);

    Task UpdateProductAsync(string id, CreateProductRequestDto createProductRequestDto);

}
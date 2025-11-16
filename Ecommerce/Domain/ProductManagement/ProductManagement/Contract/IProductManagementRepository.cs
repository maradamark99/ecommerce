using Ecommerce.Common.Pagination;

namespace Ecommerce.Domain.ProductManagement.ProductManagement.Contract;

public interface IProductManagementRepository
{
    Task<Paged<Product>> GetAllAsync(Pager pager, Sorter sorter);
    
    Task<Product?> GetByIdAsync(string id);
    
    Task<string> CreateAsync(Product product);
    
    Task<bool> ExistsByIdAsync(string id);
    
    Task DeleteByIdAsync(string id);
    
    Task UpdateAsync(string id, Product updatedProduct);
}
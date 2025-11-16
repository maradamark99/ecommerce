using EcommerceLib.Pagination;
using ProductManagement.Model;
using Attribute = ProductManagement.Model.Attribute;

namespace ProductManagement.Contract;

public interface IProductManagementRepository
{
    Task<Paged<Product>> GetAllAsync(Pager pager, Sorter sorter);
    
    Task<Product?> GetByIdAsync(string id);
    
    Task<string> CreateAsync(Product product);
    
    Task<bool> ExistsByIdAsync(string id);
    
    Task DeleteByIdAsync(string id);
    
    Task UpdateAsync(string id, Product updatedProduct);

    Task<List<Attribute>> GetExistingAttributesAsync(List<Attribute> attributes);
}
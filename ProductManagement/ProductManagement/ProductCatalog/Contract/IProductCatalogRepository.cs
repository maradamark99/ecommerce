using EcommerceLib.Pagination;
using ProductManagement.Model;

namespace ProductManagement.ProductCatalog.Contract;

public interface IProductCatalogRepository
{
    Task<Paged<Product>> GetProductsAsync(ProductFilter filter, Pager pager, Sorter sorter);
    
    Task<Product?> GetProductByIdAsync(string id);
    
    Task<IEnumerable<Product>> GetProductsByIdsAsync(string[] ids);
}
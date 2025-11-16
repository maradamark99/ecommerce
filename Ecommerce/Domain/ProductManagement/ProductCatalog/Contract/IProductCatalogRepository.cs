using Ecommerce.Common.Pagination;

namespace Ecommerce.Domain.ProductManagement.ProductCatalog.Contract;

public interface IProductCatalogRepository
{
    Task<Paged<Product>> GetProductsAsync(ProductFilter filter, Pager pager, Sorter sorter);
    
    Task<Product?> GetProductByIdAsync(string id);
}
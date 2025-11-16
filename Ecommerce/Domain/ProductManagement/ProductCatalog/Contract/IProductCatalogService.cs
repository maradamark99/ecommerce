using Ecommerce.Common.Pagination;

namespace Ecommerce.Domain.ProductManagement.ProductCatalog.Contract;

public interface IProductCatalogService
{

    Task<Paged<ProductCatalogResponse>> GetProductsAsync(ProductFilter filter, Pager pager, Sorter sorter);
    
    Task<ProductCatalogResponse> GetProductByIdAsync(string id);
    
}
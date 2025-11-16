using EcommerceLib.Pagination;

namespace ProductManagement.ProductCatalog.Contract;

public interface IProductCatalogService
{

    Task<Paged<ProductCatalogResponse>> GetProductsAsync(ProductFilter filter, Pager pager, Sorter sorter);
    
    Task<ProductCatalogResponse> GetProductByIdAsync(string id);

    Task<IEnumerable<ProductCatalogResponse>> GetProductsByIdsAsync(string[] ids);
}
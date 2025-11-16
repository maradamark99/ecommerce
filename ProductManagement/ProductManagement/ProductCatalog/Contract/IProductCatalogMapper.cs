using ProductManagement.Model;

namespace ProductManagement.ProductCatalog.Contract;

public interface IProductCatalogMapper
{
    ProductCatalogResponse ModelToResponse(Product model);
}
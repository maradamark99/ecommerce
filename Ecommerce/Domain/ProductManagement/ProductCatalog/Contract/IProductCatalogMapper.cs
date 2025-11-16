namespace Ecommerce.Domain.ProductManagement.ProductCatalog.Contract;

public interface IProductCatalogMapper
{
    ProductCatalogResponse ModelToResponse(Product model);
}
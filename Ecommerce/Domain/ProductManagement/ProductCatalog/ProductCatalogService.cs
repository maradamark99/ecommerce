using Ecommerce.Common.Exception;
using Ecommerce.Common.Pagination;
using Ecommerce.Domain.ProductManagement.ProductCatalog.Contract;

namespace Ecommerce.Domain.ProductManagement.ProductCatalog;

public class ProductCatalogService(IProductCatalogRepository productCatalogRepository, IProductCatalogMapper mapper) : IProductCatalogService
{
    public async Task<Paged<ProductCatalogResponse>> GetProductsAsync(ProductFilter filter, Pager pager, Sorter sorter)
    {
        var products = await productCatalogRepository.GetProductsAsync(filter, pager, sorter);
        return products.Select(mapper.ModelToResponse);
    }

    public async Task<ProductCatalogResponse> GetProductByIdAsync(string id)
    {
        var product = await productCatalogRepository.GetProductByIdAsync(id);
        if (product is null)
        {
            throw new NotFoundException($"Product with id: {id} not found");
        }
        return mapper.ModelToResponse(product);
    }
}
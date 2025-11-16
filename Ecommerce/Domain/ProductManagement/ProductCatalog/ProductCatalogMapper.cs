using Ecommerce.Common.Storage;
using Ecommerce.Domain.ProductManagement.ProductCatalog.Contract;
using Microsoft.Extensions.Options;

namespace Ecommerce.Domain.ProductManagement.ProductCatalog;

public class ProductCatalogMapper(IOptions<StorageConfig> config) : IProductCatalogMapper
{
    public ProductCatalogResponse ModelToResponse(Product model)
    {
        var primaryImage = model
            .Media?
            .FirstOrDefault(m => m.IsPrimaryImage);
        var primaryImageUrl = primaryImage is null ? "placeholder" :
            $"{config.Value.Endpoint}/{primaryImage.Bucket}/{primaryImage.Key}";
        var mediaUrls = model.Media!
            .Where(m => !m.IsPrimaryImage)
            .Select(m => $"{config.Value.Endpoint}/{m.Bucket}/{m.Key}")
            .ToList();
        var attributes = model.Attributes
            .Select(attribute => (attribute.Name, attribute.Value))
            .ToDictionary();
        return new ProductCatalogResponse(
            model.Id.ToString(),
            model.Name,
            model.Description,
            model.ProductCondition.ToString(),
            primaryImageUrl,
            model.Category.Id,
            attributes,
            mediaUrls,
            model.ProductListings!.First().PriceInEur
        );
    }
}
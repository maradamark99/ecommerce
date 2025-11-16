using EcommerceLib.Storage;
using Microsoft.Extensions.Options;
using ProductManagement.Model;
using ProductManagement.ProductCatalog.Contract;

namespace ProductManagement.ProductCatalog;

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
            .Select(x => (x.Name, x.Value))
            .ToDictionary();
        var listing = model.ProductListings!.First();
        return new ProductCatalogResponse(
            model.Id,
            model.Name,
            model.Description,
            model.Condition.ToString(),
            primaryImageUrl,
            model.Category.Id,
            attributes,
            mediaUrls,
            listing.Price,
            model.IsInStock,
            listing.IsDiscounted,
            listing.DiscountedPrice
        );
    }
}
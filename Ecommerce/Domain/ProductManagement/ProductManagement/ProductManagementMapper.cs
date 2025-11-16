using Ecommerce.Common.Storage;
using Ecommerce.Domain.ProductManagement.ProductManagement.Contract;
using Microsoft.Extensions.Options;

namespace Ecommerce.Domain.ProductManagement.ProductManagement;

public class ProductManagementMapper(IOptions<StorageConfig> config) : IProductManagementMapper
{
    public Product RequestToModel(CreateProductRequestDto createProductRequestDto, Category.Category category)
    {
        var attrs = createProductRequestDto.Attributes
            .Select(attr => ProductAttribute.Builder()
                .WithName(attr.Name)
                .WithValue(attr.Value)
                .Build())
            .ToList();
        return new Product(createProductRequestDto.Name, createProductRequestDto.Description, createProductRequestDto.ProductCondition, category, attrs);
    }

    public ProductManagementResponse ModelToResponse(
        Product product)
    {
        var attrs = product.Attributes.Select(attr => new ProductAttributeResponse(attr.Name, attr.Value));
        var primaryImage = product.Media?.FirstOrDefault(m => m.IsPrimaryImage);
        var primaryImageUrl = primaryImage != null ? $"{config.Value.Endpoint}/{primaryImage.Bucket}/{primaryImage.Key}" : string.Empty;
        var mediaUrls = product.Media?.Select(m => $"{config.Value.Endpoint}/{m.Bucket}/{m.Key}");
        return new ProductManagementResponse(
            product.Id.ToString(), 
            product.Name, 
            product.Description, 
            product.ProductCondition, 
            product.Category.Id, 
            attrs,
            primaryImageUrl,
            mediaUrls
        );
    }
}
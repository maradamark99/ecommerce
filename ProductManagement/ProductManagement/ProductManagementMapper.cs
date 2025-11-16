using EcommerceLib.Storage;
using Microsoft.Extensions.Options;
using ProductManagement.Contract;
using ProductManagement.Model;
using Attribute = ProductManagement.Model.Attribute;

namespace ProductManagement;

public class ProductManagementMapper(IOptions<StorageConfig> config) : IProductManagementMapper
{
    public Product RequestToModel(CreateProductRequestDto createProductRequestDto, Category.Category category, List<Attribute> attributes)
    {
        return new Product(createProductRequestDto.Name, createProductRequestDto.Description, Enum.Parse<Condition>(createProductRequestDto.ProductCondition, true), category, attributes);
    }

    public ProductManagementResponse ModelToResponse(
        Product product)
    {
        var attrs = product.Attributes.Select(x => new ProductAttributeResponse(x.Name, x.Value));
        var primaryImage = product.Media?.FirstOrDefault(m => m.IsPrimaryImage);
        var primaryImageUrl = primaryImage != null ? $"{config.Value.Endpoint}/{primaryImage.Bucket}/{primaryImage.Key}" : string.Empty;
        var mediaUrls = product.Media?.Select(m => $"{config.Value.Endpoint}/{m.Bucket}/{m.Key}");
        return new ProductManagementResponse(
            product.Id, 
            product.Name, 
            product.Description, 
            product.Condition.ToString(), 
            product.Category.Id, 
            attrs,
            primaryImageUrl,
            mediaUrls
        );
    }
}
namespace Ecommerce.Domain.ProductManagement.ProductManagement.Contract;

public record ProductManagementResponse(
    string Id, 
    string Name, 
    string Description, 
    ProductCondition ProductCondition, 
    long CategoryId, 
    IEnumerable<ProductAttributeResponse> Attributes,
    string? PrimaryImageUrl,
    IEnumerable<string>? MediaUrls
);
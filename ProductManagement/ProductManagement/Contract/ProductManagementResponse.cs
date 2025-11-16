using ProductManagement.Model;

namespace ProductManagement.Contract;

public record ProductManagementResponse(
    string Id, 
    string Name, 
    string Description, 
    string Condition, 
    long CategoryId, 
    IEnumerable<ProductAttributeResponse> Attributes,
    string? PrimaryImageUrl,
    IEnumerable<string>? MediaUrls
);
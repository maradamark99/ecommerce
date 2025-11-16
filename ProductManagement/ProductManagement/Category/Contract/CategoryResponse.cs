namespace ProductManagement.Category.Contract;

public record CategoryResponse(
    long Id,
    string Name,
    string Path,
    AttributeDefinitionResponse[]? Attributes
);
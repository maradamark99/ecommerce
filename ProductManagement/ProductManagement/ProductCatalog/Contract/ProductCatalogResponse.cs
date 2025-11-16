namespace ProductManagement.ProductCatalog.Contract;

public record ProductCatalogResponse(
    string Id,
    string Name,
    string Description, 
    string Condition,
    string PrimaryImageUrl,
    long CategoryId,
    Dictionary<string, string> Attributes, 
    List<string> ImageUrls,
    decimal Price,
    bool IsAvailable,
    bool IsDiscounted,
    decimal? DiscountedPrice
);
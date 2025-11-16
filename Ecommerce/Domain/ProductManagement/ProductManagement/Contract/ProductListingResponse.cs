namespace Ecommerce.Domain.ProductManagement.ProductManagement.Contract;

public record ProductListingResponse
(
    long Id,
    decimal PriceInEur,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string ProductId,
    string ProductName,
    string ProductDescription
);
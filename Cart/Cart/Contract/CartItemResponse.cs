namespace Cart.Contract;

public record CartItemResponse
(
    string ProductId, 
    string ProductName,
    int Quantity, 
    string Condition,
    decimal UnitPrice,
    string ImageUrl,
    bool IsDiscounted,
    decimal? DiscountedPrice,
    bool IsAvailable
);

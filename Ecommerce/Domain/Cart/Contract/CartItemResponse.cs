namespace Ecommerce.Domain.Cart.Contract;

public record CartItemResponse
(
    string ProductId, 
    string ProductName,
    int Quantity, 
    string Condition,
    decimal UnitPrice,
    string ImageUrl
);

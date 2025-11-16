namespace Ecommerce.Domain.Cart.Contract;

public record CartItem(
    string ProductId,
    int Quantity
);
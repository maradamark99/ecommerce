namespace Ecommerce.Domain.Cart.Contract;

public record CartResponse(string CartId, List<CartItemResponse> Products)
{
    public decimal TotalPrice =>
        Products.Sum(i => i.UnitPrice * i.Quantity);
}
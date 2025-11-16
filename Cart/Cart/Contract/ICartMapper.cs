namespace Cart.Contract;

public interface ICartMapper
{
    CartItemResponse ModelToResponse(CartItem cartItem, ProductDto product);
}
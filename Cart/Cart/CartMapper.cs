using Cart.Contract;

namespace Cart;

public class CartMapper : ICartMapper
{
    public CartItemResponse ModelToResponse(CartItem cartItem, ProductDto product)
    {
        return new CartItemResponse
        (
            cartItem.ProductId,
            product.Name,
            cartItem.Quantity,
            product.Condition,
            product.DiscountedPrice ?? product.Price,
            product.PrimaryImageUrl,
            product.IsDiscounted,
            product.DiscountedPrice,
            product.IsAvailable
        );
    }
}
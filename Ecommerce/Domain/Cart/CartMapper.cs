using Ecommerce.Domain.Cart.Contract;
using Ecommerce.Domain.ProductManagement;
using Ecommerce.Domain.ProductManagement.ProductCatalog.Contract;

namespace Ecommerce.Domain.Cart;

public class CartMapper : ICartMapper
{
    public CartItemResponse ModelToResponse(CartItem cartItem, ProductCatalogResponse product)
    {
        return new CartItemResponse
        (
            cartItem.ProductId,
            product.Name,
            cartItem.Quantity,
            product.Condition,
            product.Price,
            product.PrimaryImageUrl
        );
    }
}
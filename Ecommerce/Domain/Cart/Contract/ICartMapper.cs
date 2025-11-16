using Ecommerce.Domain.ProductManagement.ProductCatalog.Contract;

namespace Ecommerce.Domain.Cart.Contract;

public interface ICartMapper
{
    CartItemResponse ModelToResponse(CartItem cartItem, ProductCatalogResponse product);
}
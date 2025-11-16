using Ecommerce.Domain.Cart.Contract;

namespace Ecommerce.Domain.Cart;

public record Cart(string CustomerId, List<CartItem>? Items = null);

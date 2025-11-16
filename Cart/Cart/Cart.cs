using System.Collections.Immutable;

namespace Cart;

public record Cart(string CustomerId, List<CartItem>? Items = null);
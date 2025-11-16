using System.Collections.Concurrent;
using Cart.Contract;

namespace Cart
{
    public class InMemoryCartStore : ICartStore
    {
        private readonly ConcurrentDictionary<string, Cart> _carts = new();
        
        public IEnumerable<CartItem> GetCartForCustomer(string customerId)
        {
            if (!_carts.TryGetValue(customerId, out var cart))
            {
                return [];
            } 
            return cart.Items ?? [];
        }
        
        public bool TryModifyCart(string customerId, List<CartItem> items)
        {
            var itemsWithPositiveQuantity = items.Where(i => i.Quantity > 0).ToList();
            _carts.AddOrUpdate(customerId, 
                new Cart(customerId, itemsWithPositiveQuantity), 
                (_, _) => new Cart(customerId, itemsWithPositiveQuantity)); 
            return true;
        }

        public bool TryClearCart(string customerId)
        {
            try
            {
                _carts.AddOrUpdate(
                    customerId,
                    _ => throw new InvalidOperationException("Cart does not exist"),
                    (key, _) => new Cart(key)
                );
                return true;
            } catch (InvalidOperationException)
            {
                return false;
            }
        }
        
        public bool TryDeleteCart(string customerId)
        {
            return _carts.TryRemove(customerId, out _);
        }
    }
}

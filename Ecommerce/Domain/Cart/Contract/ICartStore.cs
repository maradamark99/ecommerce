namespace Ecommerce.Domain.Cart.Contract;

public interface ICartStore
{   
    public bool TryModifyCart(string customerId, List<CartItem> items);
    public IEnumerable<CartItem> GetCartForCustomer(string customerId);
    public bool TryClearCart(string customerId);
}
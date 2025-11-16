namespace Ecommerce.Domain.Cart.Contract;

public interface ICartService
{
    Task<CartResponse> GetByIdAsync(string userId);

    Task ModifyCartAsync(string userId, List<CartItem> items);
    
    Task ClearCartAsync(string userId);
}
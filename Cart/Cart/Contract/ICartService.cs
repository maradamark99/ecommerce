using EcommerceLib.Auth;

namespace Cart.Contract;

public interface ICartService
{
    Task<CheckoutResponseDto> CheckoutAsync(AppUser user, CheckoutRequestDto dto);
    
    Task<CartResponse> GetByIdAsync(string customerId);

    Task ModifyCartAsync(string customerId, List<CartItem> items);

    Task ClearCartAsync(string customerId);
}
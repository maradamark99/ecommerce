using EcommerceLib.Auth;
using EcommerceLib.Contract.Events;

namespace OrderManagement.Contract;

public interface IOrderManagementService
{
    Task CreateOrderAsync(CartCheckedOutEventDto cartEventDto);

    Task UpdateStatusAsync(string orderId, Status newStatus);
    
    Task CancelOrderAsync(AppUser user, string orderId, string? reason);
    
    Task FulfillOrderAsync(string orderId);
    
    Task<Order> GetByIdAsync(string customerId, string? checkoutId, string? orderId);
    
    Task<IEnumerable<Order>> GetOrderHistoryAsync(AppUser customer);
}
using EcommerceLib;

namespace OrderManagement.Contract;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(string customerId, string? checkoutId, string? orderId);

    Task<Order?> GetByIdAsync(string orderId);

    Task<string> CreateOrderAsync(Order order);
    
    Task UpdateOrderAsync(Order order);
    
    Task<IEnumerable<Order>> GetOrderHistoryAsync(string customerId);
}
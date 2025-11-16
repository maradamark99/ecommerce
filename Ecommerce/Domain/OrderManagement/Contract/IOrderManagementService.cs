namespace Ecommerce.Domain.OrderManagement.Contract;

public interface IOrderManagementService
{
    public Task<OrderSummary> CreateAsync(string customerId, CreateOrderRequest checkoutRequest);
    
    public Task CancelOrderAsync(string userId, string orderId);
    
    public Task MarkOrderAsExpiredAsync(string orderId);
    
    public Task<Order?> GetByIdAsync(string customerId, string orderId);
    
    public Task<Order?> GetByIdAsync(string orderId);
    
    public Task UpdateOrderAsync(Order order);
    
    public Task FulfillOrderAsync(string orderId);
    
    Task<IEnumerable<Order>> GetOrderHistoryAsync(string customerId);
}
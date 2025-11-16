namespace Ecommerce.Domain.OrderManagement.Contract;

public interface IOrderRepository
{
    public Task<string> CreateOrderAsync(Order order);
    
    public Task FulfillOrderAsync(Order order);
    
    public Task UpdateOrderAsync(Order order);
    
    public Task<Order?> GetByIdAsync(string orderId);
    
    public Task<IEnumerable<Order>> GetOrderHistoryAsync(string customerId);
}
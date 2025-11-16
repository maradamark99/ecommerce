namespace Payment.Contract;

public interface IPaymentRepository
{ 
    Task<Model.Payment?> GetPaymentByIdAsync(string id);
    
    Task CreatePaymentAsync(Model.Payment payment);
    
    Task UpdatePaymentAsync(Model.Payment payment);
    
    Task CreateOrderAsync(Order order);
    
    Task <Order?> GetOrderAsync(string customerId, string orderId);
    
    Task RemoveOrderAsync(string customerId, string orderId);
}
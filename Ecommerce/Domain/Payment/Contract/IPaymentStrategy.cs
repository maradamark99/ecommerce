using Ecommerce.Domain.OrderManagement;

namespace Ecommerce.Domain.Payment.Contract;

public interface IPaymentStrategy
{
    Task<PaymentInitiationResult> InitiatePaymentAsync(string userId, PaymentRequest paymentRequest);   
    
    Task CreateRefundAsync(Order order);
}
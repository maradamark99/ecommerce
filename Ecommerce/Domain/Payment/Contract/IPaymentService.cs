using Ecommerce.Domain.OrderManagement;

namespace Ecommerce.Domain.Payment.Contract;

public interface IPaymentService
{
    Task<PaymentRate> GetPaymentRateAsync(PaymentMethod paymentMethod);
    
    Task<PaymentInitiationResult> InitiatePaymentAsync(string userId, PaymentRequest paymentRequest);
    
    Task CreateRefundAsync(Order order);
}
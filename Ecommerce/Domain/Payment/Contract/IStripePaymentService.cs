using Ecommerce.Domain.OrderManagement;

namespace Ecommerce.Domain.Payment.Contract;

public interface IStripePaymentService
{
    Task<PaymentIntentResult> CreatePaymentIntentAsync(Order order);
    
    Task CreateRefundAsync(Order order);    
    
    Task HandleWebhookEventAsync(string eventBody, string signature);
}
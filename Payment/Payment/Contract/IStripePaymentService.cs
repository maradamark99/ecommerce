using Payment.Model;

namespace Payment.Contract;

public interface IStripePaymentService
{
    Task<PaymentIntentResult> CreatePaymentIntentAsync(Order order, PaymentRequest paymentRequest);
    
    Task CreateRefundAsync(Order order, RefundRequest refundRequest);    
    
    Task HandleWebhookEventAsync(string eventBody, string signature);
}
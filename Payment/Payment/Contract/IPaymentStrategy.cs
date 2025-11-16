using Payment.Model;

namespace Payment.Contract;

public interface IPaymentStrategy
{
    Task<PaymentInitiationResult> InitiatePaymentAsync(Order order, PaymentRequest paymentRequest);
    
    Task CreateRefundAsync(Order order, RefundRequest refundRequest);
}
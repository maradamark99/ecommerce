using EcommerceLib.Contract.Dto;
using Payment.Model;

namespace Payment.Contract;

public interface IPaymentService
{
    Task<PaymentFee> GetPaymentFeeAsync(PaymentMethod paymentMethod);
    
    Task<PaymentInitiationResult> InitiatePayment(PaymentRequest paymentRequest);
    
    Task CreateOrderAsync(Order order);
    
    Task UpdatePaymentStatusAsync(string paymentId, PaymentStatus status);
    
    Task HandleOrderCompletedAsync(OrderDto orderDto);
    
    Task HandleOrderCancelledAsync(OrderDto orderDto);
}
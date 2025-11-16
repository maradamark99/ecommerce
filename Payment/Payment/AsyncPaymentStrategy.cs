using Payment.Contract;
using Payment.Model;

namespace Payment;

public class AsyncPaymentStrategy(
    IPaymentRepository paymentRepository,
    IStripePaymentService stripePaymentService) : IPaymentStrategy
{
    public async Task<PaymentInitiationResult> InitiatePaymentAsync(Order order, PaymentRequest paymentRequest)
    {
       
        var paymentIntentResult = await stripePaymentService.CreatePaymentIntentAsync(order, paymentRequest);
        await paymentRepository.CreatePaymentAsync(new Model.Payment()
        {
            Id = paymentIntentResult.PaymentIntentId,
            Status = nameof(PaymentStatus.Pending), 
            Order = order
        });
        return new PaymentInitiationResult
        {
            ClientSecret = paymentIntentResult.ClientSecret
        };
    }

    public Task CreateRefundAsync(Order order, RefundRequest refundRequest)
    {
        return stripePaymentService.CreateRefundAsync(order, refundRequest);
    }
}
using Ecommerce.Domain.OrderManagement;
using Ecommerce.Domain.OrderManagement.Contract;
using Ecommerce.Domain.Payment.Contract;

namespace Ecommerce.Domain.Payment;

public class AsyncPaymentStrategy(
    IOrderManagementService orderManagementService,
    IStripePaymentService stripePaymentService) 
    : PaymentStrategyBase(orderManagementService)
{
    protected override async Task<PaymentInitiationResult> DoInitiatePaymentAsync(Order order)
    {
        var paymentIntentResult = await stripePaymentService.CreatePaymentIntentAsync(order);
        return new PaymentInitiationResult
        {
            PaymentIntentId = paymentIntentResult.PaymentIntentId,
            ClientSecret = paymentIntentResult.ClientSecret
        };  
    }

    protected override Task DoCreateRefundAsync(Order order)
    {
        return stripePaymentService.CreateRefundAsync(order);
    }
}
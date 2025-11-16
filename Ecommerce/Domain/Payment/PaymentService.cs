using Ecommerce.Common.Exception;
using Ecommerce.Domain.OrderManagement;
using Ecommerce.Domain.OrderManagement.Contract;
using Ecommerce.Domain.Payment.Contract;

namespace Ecommerce.Domain.Payment;

public class PaymentService(IPaymentStrategyFactory paymentStrategyFactory) : IPaymentService
{
    public async Task<PaymentInitiationResult> InitiatePaymentAsync(string userId, PaymentRequest paymentRequest)
    {
        var strategy = paymentStrategyFactory.CreatePaymentStrategy(paymentRequest.PaymentMethod);
        return await strategy.InitiatePaymentAsync(userId, paymentRequest);
    }
    
    public Task CreateRefundAsync(Order order)
    {
        var strategy = paymentStrategyFactory.CreatePaymentStrategy(order.PaymentMethod);
        return strategy.CreateRefundAsync(order);
    }
    
    public Task<PaymentRate> GetPaymentRateAsync(PaymentMethod paymentMethod)
    {
        var paymentRate = paymentMethod switch
        {
            PaymentMethod.CreditCard => new PaymentRate { Fee = 0.05m, PaymentMethod = PaymentMethod.CreditCard },
            PaymentMethod.CashOnDelivery => new PaymentRate { Fee = 0.15m, PaymentMethod = PaymentMethod.CashOnDelivery },
            _ => throw new NotSupportedException($"Payment method {paymentMethod} is not supported.")
        };
        return Task.FromResult(paymentRate);
    }
    
}
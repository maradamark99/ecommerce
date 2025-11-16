using Ecommerce.Domain.OrderManagement;
using Ecommerce.Domain.OrderManagement.Contract;

namespace Ecommerce.Domain.Payment;

public class PaymentOnDeliveryStrategy(IOrderManagementService orderManagementService) 
    : PaymentStrategyBase(orderManagementService)
{
    protected override async Task<PaymentInitiationResult> DoInitiatePaymentAsync(Order order)
    {
        await orderManagementService.FulfillOrderAsync(order.Id);
        return PaymentInitiationResult.Empty;
    }

    protected override Task DoCreateRefundAsync(Order order)
    {
        return Task.CompletedTask;
    }
}
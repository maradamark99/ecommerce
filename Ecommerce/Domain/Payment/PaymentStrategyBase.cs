using Ecommerce.Common.Exception;
using Ecommerce.Domain.OrderManagement;
using Ecommerce.Domain.OrderManagement.Contract;
using Ecommerce.Domain.Payment.Contract;

namespace Ecommerce.Domain.Payment;

public abstract class PaymentStrategyBase(IOrderManagementService orderManagementService) : IPaymentStrategy
{
    public async Task<PaymentInitiationResult> InitiatePaymentAsync(string userId, PaymentRequest paymentRequest)
    {
        var order = await orderManagementService.GetByIdAsync(paymentRequest.OrderId);
        ValidatePaymentInitiation(userId, order, paymentRequest.PaymentMethod);
        return await DoInitiatePaymentAsync(order!);
    }

    public Task CreateRefundAsync(Order order)
    {
        if (order == null)
        {
            throw new BadRequestException("Refund request cannot be null.");
        }
        return DoCreateRefundAsync(order);
    }
    
    protected abstract Task<PaymentInitiationResult> DoInitiatePaymentAsync(Order order);
    
    protected abstract Task DoCreateRefundAsync(Order order);
    
    private static void ValidatePaymentInitiation(string userId, Order? order, string paymentMethod)
    {
        if (order == null)
        {
            throw new NotFoundException("Order not found.");
        }
        if (order.AppUser.Id != userId)
        {
            throw new UnauthorizedException("Unauthorized");
        }
        if (order.PaymentMethod != paymentMethod)
        {
            throw new BadRequestException("Payment method does not match the order's payment method.");
        }
        if (order.CurrentStatus != Status.Pending) 
        {
            throw new BadRequestException("Order is not in a pending state.");
        }
    }
}
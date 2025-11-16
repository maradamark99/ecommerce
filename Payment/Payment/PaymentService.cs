using EcommerceLib.Contract.Dto;
using EcommerceLib.Exception;
using Payment.Contract;
using Payment.Model;

namespace Payment;

public class PaymentService(
    IPaymentRepository paymentRepository,   
    IServiceProvider serviceProvider) : IPaymentService
{
    
    public async Task<PaymentInitiationResult> InitiatePayment(PaymentRequest paymentRequest)
    {
        var order = await paymentRepository.GetOrderAsync(paymentRequest.CustomerId, paymentRequest.OrderId);
        if (order == null)
        {
            throw new UnauthorizedException("Customer is not authorized to access this order.");
        }
        if (order.Payment != null)
        {
            throw new BadRequestException("Payment has already been initiated for this order.");
        }
        if (order.SelectedPaymentMethod != paymentRequest.PaymentMethod) 
        {
            throw new BadRequestException("Payment method does not match the created order's payment method.");
        }
        var strategy = serviceProvider.GetRequiredKeyedService<IPaymentStrategy>(paymentRequest.PaymentMethod);
        return await strategy.InitiatePaymentAsync(order, paymentRequest);
    }
    
    public async Task CreateOrderAsync(Order order)
    {
        if (order == null)
        {
            throw new BadRequestException("Mapping cannot be null.");
        }
        if (await paymentRepository.GetOrderAsync(order.CustomerId, order.OrderId) != null)
        {
            return;
        }
        await paymentRepository.CreateOrderAsync(order);   
    }
    
    public async Task UpdatePaymentStatusAsync(string paymentId, PaymentStatus status)
    {
        var payment =  await paymentRepository.GetPaymentByIdAsync(paymentId);
        if (payment == null)
        {
            throw new NotFoundException("Payment not found.");
        }   
        payment.Status = status.ToString();
        await paymentRepository.UpdatePaymentAsync(payment);
    }

    public async Task HandleOrderCompletedAsync(OrderDto orderDto)
    {
        var order =  await paymentRepository.GetOrderAsync(orderDto.Customer.CustomerId, orderDto.OrderId);
        if (order?.Payment == null)
        {
            return;
        }
        var payment = order.Payment;
        if (order.SelectedPaymentMethod == PaymentMethod.CreditCard || payment.Status == nameof(PaymentStatus.Paid))
        {
            return;
        }
        await UpdatePaymentStatusAsync(payment.Id, PaymentStatus.Paid);
    }

    public async Task HandleOrderCancelledAsync(OrderDto orderDto)
    {
        var order = await paymentRepository.GetOrderAsync(orderDto.Customer.CustomerId, orderDto.OrderId);
        if (order?.Payment == null)
        {
            await paymentRepository.RemoveOrderAsync(orderDto.Customer.CustomerId, orderDto.OrderId);
            return;
        }
        var payment = order.Payment;
        if (payment.Status != nameof(PaymentStatus.Paid))
        {
            return;
        }
        var strategy = serviceProvider.GetRequiredKeyedService<IPaymentStrategy>(order.SelectedPaymentMethod);
        await strategy.CreateRefundAsync(order, new RefundRequest(order.CustomerId, order.OrderId));    
    }

    public Task<PaymentFee> GetPaymentFeeAsync(PaymentMethod paymentMethod)
    {
        var paymentRate = paymentMethod switch
        {
            PaymentMethod.CreditCard => new PaymentFee { Fee = 0.05m, Method = nameof(PaymentMethod.CreditCard) },
            PaymentMethod.CashOnDelivery => new PaymentFee { Fee = 0.15m, Method = nameof(PaymentMethod.CashOnDelivery) },
            _ => throw new NotFoundException($"Payment method {paymentMethod} is not supported.")
        };
        return Task.FromResult(paymentRate);
    }
    
}
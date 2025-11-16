using System.Globalization;
using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Payment.Contract;
using Payment.Model;

namespace Payment;

public class PaymentOnDeliveryStrategy(
    IEventProducer<PaymentEventDto> eventProducer,
    IPaymentRepository paymentRepository) : IPaymentStrategy
{
    public async Task<PaymentInitiationResult> InitiatePaymentAsync(Order order, PaymentRequest paymentRequest)
    {
        var payment = new Model.Payment()
        {
            Id = Guid.NewGuid().ToString(),
            Status = nameof(PaymentStatus.PendingOnDelivery),
            Order = order
        };
        await paymentRepository.CreatePaymentAsync(payment);
        await eventProducer.ProduceAsync(new PaymentEventDto()
        {
            EventId = Guid.NewGuid().ToString(),
            OrderId = paymentRequest.OrderId,
            CustomerId = paymentRequest.CustomerId,
            PaymentId = payment.Id,
            EventType = nameof(Events.PaymentPendingOnDelivery),
            Amount = order.TotalAmount,
            Timestamp = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture),
            
        });
        return PaymentInitiationResult.Empty();
    }

    public Task CreateRefundAsync(Order order, RefundRequest refundRequest)
    {
        return Task.CompletedTask;
    }
}
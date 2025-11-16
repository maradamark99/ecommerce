using EcommerceLib.Contract.Events;
using EcommerceLib.Exception;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Options;
using Payment.Contract;
using Payment.Model;
using Stripe;

namespace Payment;

public class StripePaymentService(
    IPaymentService paymentService,
    IOptions<PaymentConfig> paymentConfig,
    IEventProducer<PaymentEventDto> eventProducer) : IStripePaymentService
{
    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(Order order, PaymentRequest paymentRequest)
    {
        var options = new PaymentIntentCreateOptions()
        {
            Amount = (long)(order.TotalAmount * 100),
            Currency = "usd",
            Metadata = new Dictionary<string, string>
            {
                { "OrderId", paymentRequest.OrderId }
            }
        };
        
        var service = new PaymentIntentService();
        var paymentIntent = await service.CreateAsync(options);
        return new PaymentIntentResult()
        {
            PaymentIntentId = paymentIntent.Id,
            ClientSecret = paymentIntent.ClientSecret
        };
    }

    public async Task CreateRefundAsync(Order order, RefundRequest refundRequest)
    {
        if (refundRequest == null)
        {
            throw new BadRequestException("Refund request cannot be null.");
        }
        var options = new RefundCreateOptions
        {
            PaymentIntent = order.Payment!.Id,
            Amount = (long)(order.TotalAmount * 100),
            Currency = "usd",
            Reason = "requested_by_customer",
            Metadata = new Dictionary<string, string>
            {
                { "orderId", refundRequest.OrderId }
            }
        };
        try
        {
            var service = new RefundService();
            await service.CreateAsync(options);
            await paymentService.UpdatePaymentStatusAsync(order.Payment.Id, PaymentStatus.Refunded);
        }
        catch (StripeException)
        {
            throw new BadRequestException("An error occurred while processing the refund.");
        }
    }

    public async Task HandleWebhookEventAsync(string eventBody, string signature)
    {
        if (!ValidateWebhookSignature(eventBody, signature, out var result))
        {
            throw new BadRequestException("Invalid Stripe webhook signature.");
        }
        var stripeEvent = EventUtility.ParseEvent(result!);
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        var orderId = paymentIntent?.Metadata["OrderId"];
        if (paymentIntent == null) 
        {
            throw new BadRequestException("Payment intent data is missing in the event.");
        }
        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new BadRequestException("Order ID not found in payment intent metadata.");
        }
        switch (stripeEvent.Type)
        {
            case "payment_intent.succeeded":
                if (paymentIntent == null) 
                {
                    throw new BadRequestException("Payment intent data is missing in the event.");
                }
                if (string.IsNullOrWhiteSpace(orderId))
                {
                    throw new BadRequestException("Order ID not found in payment intent metadata.");
                }
                await paymentService.UpdatePaymentStatusAsync(paymentIntent.Id, PaymentStatus.Paid);
                await eventProducer.ProduceAsync(new PaymentEventDto
                {
                    EventId = Guid.NewGuid().ToString(),
                    PaymentId = paymentIntent.Id,
                    OrderId = orderId,
                    EventType = nameof(Events.PaymentSucceeded),
                    Timestamp = DateTime.UtcNow.ToLongDateString(),
                });
                break;

            case "payment_intent.payment_failed":
                await paymentService.UpdatePaymentStatusAsync(paymentIntent.Id, PaymentStatus.Failed);
                await eventProducer.ProduceAsync(new PaymentEventDto
                {
                    EventId = Guid.NewGuid().ToString(),
                    PaymentId = paymentIntent.Id,
                    OrderId = orderId,
                    EventType = nameof(Events.PaymentFailed),
                    Timestamp = DateTime.UtcNow.ToLongDateString(),
                    FailureReason = paymentIntent.LastPaymentError?.Message
                });
                break;

            default:
                throw new BadRequestException($"Stripe event type {stripeEvent.Type} is not supported.");
        }
    }

    private bool ValidateWebhookSignature(string json, string signature, out string? result)
    {
        try 
        {
            result = EventUtility.ConstructEvent(
                json,
                signature,
                paymentConfig.Value.StripeSecret
            ).ToJson();
            return true;
        }
        catch (StripeException)
        {
            result = null;  
            return false;
        }   
    }
    
}
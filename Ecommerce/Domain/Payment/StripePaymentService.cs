using Ecommerce.Common.Exception;
using Ecommerce.Domain.OrderManagement;
using Ecommerce.Domain.OrderManagement.Contract;
using Ecommerce.Domain.Payment.Contract;
using Microsoft.Extensions.Options;
using Stripe;
using Order = Ecommerce.Domain.OrderManagement.Order;

namespace Ecommerce.Domain.Payment;

public class StripePaymentService : IStripePaymentService
{
    private readonly IOptions<PaymentConfig> _paymentConfig;
    private readonly IOrderManagementService _orderManagementService;

    public StripePaymentService(
        IOptions<PaymentConfig> paymentConfig,
        IOrderManagementService orderManagementService)
    {
        _paymentConfig = paymentConfig;
        _orderManagementService = orderManagementService;
        StripeConfiguration.ApiKey = _paymentConfig.Value.SecretKey;
    }


    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(Order order)
    {
        var options = new PaymentIntentCreateOptions()
        {
            Amount = (long)(order.Total * 100),
            Currency = _paymentConfig.Value.Currency,
            PaymentMethodTypes = ["card"],
            Metadata = new Dictionary<string, string>
            {
                { "OrderId", order.Id }
            }
        };
        
        var service = new PaymentIntentService();
        var paymentIntent = await service.CreateAsync(options);
        order.PaymentIntentId = paymentIntent.Id;
        await _orderManagementService.UpdateOrderAsync(order);    
        return new PaymentIntentResult()
        {
            PaymentIntentId = paymentIntent.Id,
            ClientSecret = paymentIntent.ClientSecret
        };
    }

    public async Task CreateRefundAsync(Order order)
    {
        if (order == null)
        {
            throw new BadRequestException("Refund request cannot be null.");
        }
        var options = new RefundCreateOptions
        {
            PaymentIntent = order.PaymentIntentId,
            Amount = (long)(order.Total * 100),
            Currency = _paymentConfig.Value.Currency,
            Reason = "requested_by_customer",
            Metadata = new Dictionary<string, string>
            {
                { "orderId", order.Id }
            }
        };
        try
        {
            var service = new RefundService();
            await service.CreateAsync(options);
        }
        catch (StripeException)
        {
            throw new BadRequestException("An error occurred while processing the refund.");
        }
    }

    public async Task HandleWebhookEventAsync(string eventBody, string signature)
    {
        if (!ValidateWebhookSignature(eventBody, signature, out var result))
            throw new BadRequestException("Invalid Stripe webhook signature.");

        var stripeEvent = EventUtility.ParseEvent(result!, throwOnApiVersionMismatch: false);

        PaymentIntent? paymentIntent = null;
        string? orderId = null;

        switch (stripeEvent.Data.Object)
        {
            case PaymentIntent pi:
                paymentIntent = pi;
                pi.Metadata.TryGetValue("OrderId", out orderId);
                break;
            case Charge charge:
                if (!string.IsNullOrEmpty(charge.PaymentIntentId))
                {
                    var piService = new PaymentIntentService();
                    paymentIntent = await piService.GetAsync(charge.PaymentIntentId);
                    paymentIntent.Metadata.TryGetValue("OrderId", out orderId);
                }
                break;
            default:
                return;
        }
        
        if (paymentIntent == null)
            throw new BadRequestException("Payment intent data is missing in the event.");
        var order = await _orderManagementService.GetByIdAsync(orderId ?? "");
        if (order == null)
            throw new NotFoundException($"Order with ID {orderId} not found.");
        if (order.StatusHistory.Any(s => s.Status == Status.Paid))
            return;
        if (order.PaymentIntentId != paymentIntent.Id)
            throw new BadRequestException("Payment intent ID does not match the order.");

        switch (stripeEvent.Type)
        {
            case PaymentEvents.PaymentIntentSucceeded:
            case PaymentEvents.ChargeSucceeded:
                await _orderManagementService.FulfillOrderAsync(order.Id);
                break;
            case PaymentEvents.PaymentIntentFailed:
            case PaymentEvents.ChargeFailed:
            case PaymentEvents.PaymentIntentCanceled:
                await _orderManagementService.CancelOrderAsync(order.AppUser.Id, orderId!);
                break;
        }
    }

    private bool ValidateWebhookSignature(string json, string signature, out string? result)
    {
        try 
        {
            result = EventUtility.ConstructEvent(
                json,
                signature,
                _paymentConfig.Value.WebhookSecret,
                throwOnApiVersionMismatch: false
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
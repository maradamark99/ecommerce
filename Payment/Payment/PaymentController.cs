using EcommerceLib.Contract;
using EcommerceLib.Exception;
using Microsoft.AspNetCore.Mvc;
using Payment.Contract;
using Payment.Model;

namespace Payment;


[ApiController]
[Route("api/v{version:apiVersion}/payments")]
public class PaymentController(
    IPaymentService paymentService,
    IStripePaymentService stripePaymentService,
    IUserContextService userContextService) : ControllerBase
{

    [HttpGet]
    [Route("payment-methods")]
    public IActionResult GetPaymentMethods()
    {
        return Ok(new[]
        {
            nameof(PaymentMethod.CashOnDelivery),
            nameof(PaymentMethod.CreditCard)
        });
    }
    
    [HttpPost]
    [Route("initiate-payment")]
    public async Task<IActionResult> InitiatePaymentAsync([FromBody] PaymentRequestDto paymentRequestDto)
    {
        var customer = userContextService.GetUser() ?? throw new UnauthorizedException("User not authorized.");
        return Ok(await paymentService.InitiatePayment(MapToPaymentRequest(customer.Id, paymentRequestDto)));
    }
    
    [HttpPost]
    [Route("webhook")]
    public async Task<IActionResult> HandleStripePaymentAsync()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(Request.Headers["Stripe-Signature"])) 
        {
            return BadRequest("Missing Stripe-Signature header");
        }
        await stripePaymentService.HandleWebhookEventAsync(json, Request.Headers["Stripe-Signature"]!);
        return Ok();
    }
    
    [HttpGet]
    [Route("fee/{paymentMethod}")]
    public async Task<IActionResult> GetPaymentFeeAsync(string paymentMethod)
    {
        if (!Enum.TryParse<PaymentMethod>(paymentMethod, out var method))
        {
            throw new BadRequestException("Invalid payment method.");
        }
        return Ok(await paymentService.GetPaymentFeeAsync(method));
    }
    
    private static PaymentRequest MapToPaymentRequest(string customerId, PaymentRequestDto dto)
    {
        if (!Enum.TryParse<PaymentMethod>(dto.PaymentMethod, out var paymentMethod))
        {
            throw new BadRequestException("Invalid payment method.");
        }
        if (string.IsNullOrEmpty(dto.OrderId))
        {
            throw new BadRequestException("Order and order items cannot be null or empty.");
        }
        return new PaymentRequest
        {
            CustomerId = customerId,
            PaymentMethod = paymentMethod,
            OrderId = dto.OrderId
        };
    }
    
}
using Ecommerce.Domain.Auth;
using Ecommerce.Domain.Auth.Contract;
using Ecommerce.Domain.OrderManagement.Contract;
using Ecommerce.Domain.Payment.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Domain.Payment;


[ApiController]
[Route("api/v{version:apiVersion}/payments")]
public class PaymentController(
    IUserContextService userContextService,
    IPaymentService paymentService,
    IStripePaymentService stripePaymentService) : ControllerBase
{
    
    [HttpPost]
    [Route("initiate")]
    [Authorize(Roles = nameof(Roles.Customer))]
    public async Task<IActionResult> InitiatePaymentAsync([FromBody] PaymentRequestDto paymentRequestDto)
    {
        var userId = userContextService.GetUserId()
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
        var paymentRequest = new PaymentRequest
        {
            OrderId = paymentRequestDto.OrderId,
            PaymentMethod = paymentRequestDto.PaymentMethod
        };
        return Ok(await paymentService.InitiatePaymentAsync(userId, paymentRequest));
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
    
}
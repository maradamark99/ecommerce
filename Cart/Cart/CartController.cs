using Cart.Contract;
using EcommerceLib.Contract;
using EcommerceLib.Exception;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Cart;

[ApiController]
[Route("api/v{version:apiVersion}/cart")]
public class CartController(
    ICartService cartService, 
    IUserContextService userContextService,
    IValidator<CheckoutRequestDto> validator) : ControllerBase
{

    [HttpPost]
    [Route("checkout")]
    public async Task<ActionResult> CheckoutAsync([FromBody] CheckoutRequestDto dto)
    {
        var customer = userContextService.GetUser()
                       ?? throw new UnauthorizedAccessException("User is not authenticated.");
        var validationResult = await validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errorResponse = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
            return BadRequest(new
            {
                message = "Validation failed for checkout request.",
                errors = errorResponse
            });
        }
        return Accepted(await cartService.CheckoutAsync(customer, dto));
    } 
    
    [HttpGet]
    public async Task<ActionResult<CartResponse>> GetCartForCustomer()
    {
        var user = userContextService.GetUser() ?? throw new UnauthorizedException("Unauthorized");
        return  Ok(await cartService.GetByIdAsync(user.Id));
    }
    
    [HttpPut]
    public async Task<IActionResult> ModifyCart([FromBody] List<CartItem> items)
    {
        var user = userContextService.GetUser() ?? throw new UnauthorizedException("Unauthorized");
        await cartService.ModifyCartAsync(user.Id, items);
        return NoContent();
    }
    
    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var user = userContextService.GetUser() ?? throw new UnauthorizedException("Unauthorized");
        await cartService.ClearCartAsync(user.Id);
        return NoContent();
    }
    
}

using Ecommerce.Common.Exception;
using Ecommerce.Domain.Auth;
using Ecommerce.Domain.Auth.Contract;
using Ecommerce.Domain.Cart.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Domain.Cart;

[ApiController]
[Route("api/v{version:apiVersion}/cart")]
public class CartController(
    ICartService cartService, 
    IUserContextService userContextService) : ControllerBase
{

    [Authorize(Roles = nameof(Roles.Customer))]
    [HttpGet]
    public async Task<ActionResult<CartResponse>> GetCartForCustomer()
    {
        var userId = userContextService.GetUserId() ?? throw new UnauthorizedException("Unauthorized");
        return  Ok(await cartService.GetByIdAsync(userId));
    }
    
    [Authorize(Roles = nameof(Roles.Customer))]
    [HttpPut]
    public async Task<IActionResult> ModifyCart([FromBody] List<CartItem> items)
    {
        var userId = userContextService.GetUserId() ?? throw new UnauthorizedException("Unauthorized");
        await cartService.ModifyCartAsync(userId, items);
        return NoContent();
    }
    
    [Authorize(Roles = nameof(Roles.Customer))]
    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var userId = userContextService.GetUserId() ?? throw new UnauthorizedException("Unauthorized");
        await cartService.ClearCartAsync(userId);
        return NoContent();
    }
    
}
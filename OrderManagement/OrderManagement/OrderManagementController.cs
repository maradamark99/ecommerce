using EcommerceLib.Contract;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Contract;

namespace OrderManagement;

[ApiController]
[Route("api/v{version:apiVersion}/order-management")]
public class OrderManagementController(
    IOrderManagementService orderManagementService,
    IUserContextService userContextService) : ControllerBase
{
    
    [HttpGet]
    public async Task<IActionResult> GetByIdAsync([FromQuery] string? checkoutId, [FromQuery] string? orderId)
    {
        var user = userContextService.GetUser()
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
        if (checkoutId == null && orderId == null) 
            return BadRequest();
        return Ok(await orderManagementService.GetByIdAsync(user.Id, checkoutId, orderId));
    }
    
    [HttpDelete]
    [Route("{orderId}")]
    public async Task<IActionResult> CancelAsync(string orderId)
    {
        var user = userContextService.GetUser()
            ?? throw new UnauthorizedAccessException("User is not authenticated."); 
        await orderManagementService.CancelOrderAsync(user, orderId, "Order cancelled by customer.");
        return NoContent();
    }   
    
    [HttpGet]
    [Route("history")]
    public async Task<IEnumerable<Order>> GetOrderHistoryAsync()
    {
        var user = userContextService.GetUser() ?? throw new UnauthorizedAccessException("User is not authenticated.");
        return await orderManagementService.GetOrderHistoryAsync(user);
    }
    
}
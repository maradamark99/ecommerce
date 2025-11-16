using Ecommerce.Domain.Auth;
using Ecommerce.Domain.Auth.Contract;
using Ecommerce.Domain.OrderManagement.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Domain.OrderManagement;

[ApiController]
[Route("api/v{version:apiVersion}/order-management")]
public class OrderManagementController(
    IOrderDataMapper orderDataMapper, 
    IOrderManagementService orderManagementService,
    IUserContextService userContextService) : ControllerBase
{
    [Authorize(Roles = nameof(Roles.Customer))]
    [HttpPost]
    public async Task<ActionResult<OrderSummaryResponseDto>> CreateAsync(CreateRequestDto createRequestDto)
    {
        var customerId = userContextService.GetUserId() 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
        var checkoutOrderRequest = orderDataMapper.MapCreateRequestDtoToModel(createRequestDto);
        var orderSummary = await orderManagementService.CreateAsync(customerId, checkoutOrderRequest);
        
        return CreatedAtAction(nameof(GetByIdAsync), new { id = orderSummary.OrderId }, orderSummary);
    }
    
    [Authorize(Roles = nameof(Roles.Customer))]
    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetByIdAsync(string id)
    {
        var userId = userContextService.GetUserId() 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
        return Ok(orderDataMapper.MapOrderToResponseDto(await orderManagementService.GetByIdAsync(userId, id)));
    }
    
    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.Customer)}")]
    [HttpDelete]
    [Route("{orderId}")]
    public async Task<IActionResult> CancelAsync(string orderId)
    {
        var userId = userContextService.GetUserId() 
            ?? throw new UnauthorizedAccessException("User is not authenticated."); 
        await orderManagementService.CancelOrderAsync(userId, orderId);
        return NoContent();
    }   
    
    [Authorize(Roles = nameof(Roles.Customer))]
    [HttpGet]
    [Route("history")]
    public async Task<IEnumerable<Order>> GetOrderHistoryAsync()
    {
        var customerId = userContextService.GetUserId() ?? throw new UnauthorizedAccessException("User is not authenticated.");
        return await orderManagementService.GetOrderHistoryAsync(customerId);
    }
    
}
using Inventory.Contract;
using Microsoft.AspNetCore.Mvc;

namespace Inventory;

[ApiController]
[Route("api/v{version:apiVersion}/inventory")]
public class InventoryController(IInventoryService inventoryService) : ControllerBase
{
    [HttpPost]
    [Route("reserve-stock")]
    public async Task<IActionResult> TryReserveStockAsync([FromBody] StockReservationRequest stockReservationRequest)
    {
        var results = await inventoryService.TryReserveStockAsync(stockReservationRequest);
        return Ok(results);
    }
    
    [HttpPut]
    [Route("update-stock")]
    public async Task<IActionResult> UpdateStockAsync([FromBody] UpdateStockRequest request)
    {
        if (!Enum.TryParse<Operation>(request.Operation, true, out var operation))
        {
            return BadRequest();
        }
        await inventoryService.UpdateStockAsync(request.ProductId, request.Quantity, operation);
        return NoContent();
    }
}
using Microsoft.AspNetCore.Mvc;
using ProductManagement.Contract;

namespace ProductManagement;

[ApiController]
[Route("api/v{version:apiVersion}/product-management/listings")]
public class ProductListingController(IProductListingService productListingService) : ControllerBase
{

    [HttpGet]
    [Route("{productId}")]
    public async Task<IActionResult> GetProductListingsHistory(string productId)
    {
        return Ok(await productListingService.GetProductListingHistoryAsync(productId));
    }

    [HttpPost]
    [Route("")]
    public async Task<IActionResult> ListProduct([FromBody] ProductListingRequest productListing)
    {
        await productListingService.ListProductAsync(productListing);
        return Ok();
    }

    [HttpDelete]
    [Route("{productId}")]
    public async Task<IActionResult> DelistProduct(string productId)
    {
        await productListingService.DelistProductAsync(productId);
        return NoContent();
    }
    
    [HttpPut]
    [Route("{productId}/discount")]
    public async Task<IActionResult> ApplyDiscount(string productId, [FromBody] DiscountRequest discountRequest)
    {
        await productListingService.ApplyDiscountAsync(productId, discountRequest);
        return NoContent();
    }
    
}
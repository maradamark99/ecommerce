using Ecommerce.Domain.Auth;
using Ecommerce.Domain.ProductManagement.ProductManagement.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Domain.ProductManagement.ProductManagement;

[ApiController]
[Route("api/v{version:apiVersion}/product-management/listing")]
public class ProductListingController(IProductListingService productListingService) : ControllerBase
{

    [Authorize(Roles = nameof(Roles.Admin))]
    [HttpGet]
    [Route("{productId}")]
    public async Task<IActionResult> GetProductListingsHistory(string productId)
    {
        return Ok(await productListingService.GetProductListingHistoryAsync(productId));
    }

    [Authorize(Roles = nameof(Roles.Admin))]
    [HttpPost]
    public async Task<IActionResult> ListProduct([FromBody] ProductListingRequest productListing)
    {
        await productListingService.ListProductAsync(productListing);
        return Ok();
    }

    [Authorize(Roles = nameof(Roles.Admin))]
    [HttpDelete]
    [Route("{productId}")]
    public async Task<IActionResult> DelistProduct(string productId)
    {
        await productListingService.DelistProductAsync(productId);
        return NoContent();
    }
    
}
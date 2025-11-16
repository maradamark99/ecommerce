using EcommerceLib.Contract;
using EcommerceLib.Contract.Dto;
using Microsoft.AspNetCore.Mvc;
using Shipping.Contract;

namespace Shipping;

[ApiController]
[Route("api/v{version:apiVersion}/shipping")]
public class ShippingController(IShippingService shippingService) : ControllerBase
{
    [HttpGet]
    [Route("fee/{shippingMethod}")]
    public async Task<ActionResult<ShippingRateDto>> GetShippingFeeAsync(string shippingMethod)
    {
        if (!Enum.TryParse<ShippingMethod>(shippingMethod, ignoreCase: true, out var shippingMethodEnum))
        {
            return BadRequest("Invalid shipping method.");
        }
        return Ok(await shippingService.GetShippingFeeAsync(shippingMethodEnum));
    }
    
}

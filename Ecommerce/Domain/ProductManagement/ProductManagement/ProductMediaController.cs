using Ecommerce.Domain.Auth;
using Ecommerce.Domain.ProductManagement.ProductManagement.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Domain.ProductManagement.ProductManagement;

[ApiController]
[Route("api/v{version:apiVersion}/product-management/media")]
public class ProductMediaController(IProductMediaService productMediaService) : ControllerBase
{
    
    [Authorize(Roles = nameof(Roles.Admin))]
    [HttpPost]
    [Route("{productId}")]
    public async Task<ActionResult<string>> AddMediaAsync(string productId, [FromQuery] bool isPrimaryImage, IFormFile file)
    {
        var createdMedia = await productMediaService.AddMediaAsync(productId, file, isPrimaryImage);
        return Created(createdMedia.Url, createdMedia);
    }

    [Authorize(Roles = nameof(Roles.Admin))]
    [HttpDelete]
    [Route("{productId}/{mediaId:long}")]
    public async Task<IActionResult> DeleteMediaAsync(string productId, long mediaId) {
        await productMediaService.RemoveMediaAsync(productId, mediaId);
        return NoContent();
    }
}
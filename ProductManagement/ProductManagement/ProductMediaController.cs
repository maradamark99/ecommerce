using Microsoft.AspNetCore.Mvc;
using ProductManagement.Contract;

namespace ProductManagement;

[ApiController]
[Route("api/v{version:apiVersion}/product-management/media")]
public class ProductMediaController(IProductMediaService productMediaService) : ControllerBase
{
    
    [HttpPost]
    [Route("{productId}")]
    public async Task<ActionResult<string>> AddMediaAsync(string productId, [FromQuery] bool isPrimaryImage, IFormFile file)
    {
        var createdMedia = await productMediaService.AddMediaAsync(productId, file, isPrimaryImage);
        return Created(createdMedia.Url, createdMedia);
    }

    [HttpDelete]
    [Route("{productId}/{mediaId:long}")]
    public async Task<IActionResult> DeleteMediaAsync(string productId, long mediaId) {
        await productMediaService.RemoveMediaAsync(productId, mediaId);
        return NoContent();
    }
}
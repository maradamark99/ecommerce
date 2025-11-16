using EcommerceLib.Pagination;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.Contract;

namespace ProductManagement;

[ApiController]
[Route("api/v{version:apiVersion}/product-management")]
public class ProductManagementController(IProductManagementService productManagementService) : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<Paged<ProductManagementResponse>>> GetAllProductsAsync([FromQuery] Pager pager, [FromQuery] Sorter sorter)
    {
        return Ok(await productManagementService.GetAllProductsAsync(pager, sorter));
    } 

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetProductById(string id) {
        return Ok(await productManagementService.GetProductByIdAsync(id));
    }

    [HttpPost]
    public async Task<IActionResult> CreateProductAsync([FromBody] CreateProductRequestDto createProduct)
    {
        var createdId = await productManagementService.CreateProductAsync(createProduct);
        return CreatedAtAction(nameof(GetProductById), new { id = createdId }, new { id = createdId });
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> DeleteProductByIdAsync(string id)
    {
        await productManagementService.DeleteProductByIdAsync(id);
        return NoContent();
    }
    
    [HttpPut]
    [Route("{id}")]
    public async Task<IActionResult> UpdateProductAsync(string id, [FromBody] CreateProductRequestDto createProduct)
    {
        await productManagementService.UpdateProductAsync(id, createProduct);
        return NoContent();
    }
    
}


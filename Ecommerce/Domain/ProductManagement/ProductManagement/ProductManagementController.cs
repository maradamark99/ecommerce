using Ecommerce.Common.Pagination;
using Ecommerce.Domain.Auth;
using Ecommerce.Domain.ProductManagement.ProductManagement.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Domain.ProductManagement.ProductManagement;

[ApiController]
[Route("api/v{version:apiVersion}/product-management")]
public class ProductManagementController(IProductManagementService productManagementService) : ControllerBase
{

    [Authorize(Roles = nameof(Roles.Admin))]
    [HttpGet]
    public async Task<ActionResult<Paged<ProductManagementResponse>>> GetAllProductsAsync([FromQuery] Pager pager, [FromQuery] Sorter sorter)
    {
        return Ok(await productManagementService.GetAllProductsAsync(pager, sorter));
    } 

    [Authorize(Roles = nameof(Roles.Admin))]
    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetProductById(string id) {
        return Ok(await productManagementService.GetProductByIdAsync(id));
    }

    [Authorize(Roles = nameof(Roles.Admin))]
    [HttpPost]
    public async Task<IActionResult> CreateProductAsync([FromBody] CreateProductRequestDto createProduct)
    {
        var createdId = await productManagementService.CreateProductAsync(createProduct);
        return CreatedAtAction(nameof(GetProductById), new { id = createdId }, new { id = createdId });
    }

    [Authorize(Roles = nameof(Roles.Admin))]
    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> DeleteProductByIdAsync(string id)
    {
        await productManagementService.DeleteProductByIdAsync(id);
        return Ok();
    }
    
    [Authorize(Roles = nameof(Roles.Admin))]
    [HttpPut]
    [Route("{id}")]
    public async Task<IActionResult> UpdateProductAsync(string id, [FromBody] CreateProductRequestDto createProduct)
    {
        await productManagementService.UpdateProductAsync(id, createProduct);
        return NoContent();
    }
    
  
}


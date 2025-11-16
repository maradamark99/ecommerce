using Ecommerce.Common.Pagination;
using Ecommerce.Domain.Auth;
using Ecommerce.Domain.ProductManagement.Category.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Domain.ProductManagement.Category;

[ApiController]
[Route("api/v{version:apiVersion}/categories")]
public class CategoryController(ICategoryService categoryService) : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<Paged<CategoryResponse>>> GetAllAsync([FromQuery] Pager pager, [FromQuery] Sorter sorter)
    {
        return Ok(await categoryService.GetAllCategoriesAsync(pager, sorter));
    }
        
    [HttpGet]
    [Route("{id:long}")]
    public async Task<IActionResult> GetByIdAsync(long id)
    {
        return Ok(await categoryService.GetCategoryByIdAsync(id));
    }
    
    [Authorize(Roles = nameof(Roles.Admin))]       
    [HttpPost]
    public async Task<ActionResult<long>> CreateCategoryAsync([FromBody] CategoryRequest category)
    {
        var createdId = await categoryService.CreateCategoryAsync(category);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = createdId }, new { id = createdId });
    }
    
    [Authorize(Roles = nameof(Roles.Admin))]       
    [HttpPut]
    [Route("{id:long}")]
    public async Task<IActionResult> UpdateCategoryAsync(long id, [FromBody] CategoryRequest category)
    {
        await categoryService.UpdateCategoryAsync(id, category);
        return Ok();
    }
    
    [Authorize(Roles = nameof(Roles.Admin))]       
    [HttpDelete]
    [Route("{id:long}")]
    public async Task<IActionResult> DeleteCategoryAsync(long id)
    {
        await categoryService.DeleteCategoryAsync(id);
        return NoContent();
    }
    
}
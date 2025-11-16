using System.Net;
using EcommerceLib.Pagination;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.Category.Contract;

namespace ProductManagement.Category;

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
    
    [HttpPost]
    public async Task<ActionResult<long>> CreateCategoryAsync([FromBody] CategoryRequest category)
    {
        var createdId = await categoryService.CreateCategoryAsync(category);
        return new JsonResult(createdId)
        {
            StatusCode = (int) HttpStatusCode.Created
        };
    }
    
    [HttpPut]
    [Route("{id:long}")]
    public async Task<IActionResult> UpdateCategoryAsync(long id, [FromBody] CategoryRequest category)
    {
        await categoryService.UpdateCategoryAsync(id, category);
        return Ok();
    }
    
    [HttpDelete]
    [Route("{id:long}")]
    public async Task<IActionResult> DeleteCategoryAsync(long id)
    {
        await categoryService.DeleteCategoryAsync(id);
        return NoContent();
    }
    
}
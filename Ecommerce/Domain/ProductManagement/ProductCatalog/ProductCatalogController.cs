using Ecommerce.Common.Pagination;
using Ecommerce.Domain.ProductManagement.ProductCatalog.Contract;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Domain.ProductManagement.ProductCatalog;

[ApiController]
[Route("api/v{version:apiVersion}/products")]
public class ProductCatalogController(IProductCatalogService catalogService) : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<Paged<ProductCatalogResponse>>> GetProducts(
        [FromQuery] ProductFilter filter, 
        [FromQuery] Pager pager, 
        [FromQuery] Sorter sorter)
    {
        return Ok(await catalogService.GetProductsAsync(filter, pager, sorter));
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<ActionResult<ProductCatalogResponse>> GetProduct(string id)
    {
        return Ok(await catalogService.GetProductByIdAsync(id));
    }
    
}
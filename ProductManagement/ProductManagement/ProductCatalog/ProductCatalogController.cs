using EcommerceLib.Pagination;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.ProductCatalog.Contract;

namespace ProductManagement.ProductCatalog;

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
    
    [HttpGet]
    [Route("batch/{ids}")]
    public async Task<ActionResult<IEnumerable<ProductCatalogResponse>>> GetProductsByIds(string ids)
    {
        return Ok(await catalogService.GetProductsByIdsAsync(ids.Split(',')));
    }

}
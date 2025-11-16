using Ecommerce.Common.Data;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Common;

[ApiController]
[Route("api/test")]
public class TestController(
    AppDbContext dbContext,
    IWebHostEnvironment hostingEnvironment) : ControllerBase
{
    
    [HttpPost]
    [Route("cleanup")]
    public IActionResult CleanUpAsync()
    {
        if (!hostingEnvironment.IsDevelopment())
        {
            return NotFound();
        }
        dbContext.AttributeDefinitions.RemoveRange(dbContext.AttributeDefinitions);
        dbContext.Categories.RemoveRange(dbContext.Categories);
        dbContext.ProductAttributes.RemoveRange(dbContext.ProductAttributes);
        dbContext.ProductMedia.RemoveRange(dbContext.ProductMedia);
        dbContext.ProductListings.RemoveRange(dbContext.ProductListings);
        dbContext.Products.RemoveRange(dbContext.Products);
        dbContext.CustomerDetails.RemoveRange(dbContext.CustomerDetails);
        dbContext.OrderItems.RemoveRange(dbContext.OrderItems);
        dbContext.Orders.RemoveRange(dbContext.Orders);
        dbContext.Reviews.RemoveRange(dbContext.Reviews);
        dbContext.WishlistItems.RemoveRange(dbContext.WishlistItems);
        dbContext.Wishlists.RemoveRange(dbContext.Wishlists);
        dbContext.SaveChanges();
        return Ok("Database cleaned up successfully.");
    }
    
}
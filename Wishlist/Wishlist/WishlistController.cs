using EcommerceLib.Contract;
using Microsoft.AspNetCore.Mvc;
using Wishlist.Contract;

namespace Wishlist
{
    [ApiController]
    [Route("api/v{version:apiVersion}/wishlist")]
    public class WishlistController(
        IWishlistService wishlistService, 
        IUserContextService userContextService) : ControllerBase
    {

        [HttpGet]
        public async Task<IActionResult> GetWishlistAsync()
        {
            var customer = userContextService.GetUser() 
                        ?? throw new UnauthorizedAccessException("User is not authenticated.");
            var wishlist = await wishlistService.GetWishlistAsync(customer);
            if (wishlist == null || !wishlist.Any())
            {
                return NoContent();
            }
            return Ok(wishlist);
        }
        
        [HttpPost]
        [Route("{productId}")]
        public async Task<IActionResult> AddToWishlistAsync([FromRoute] string productId)
        {
            var customer = userContextService.GetUser() 
                         ?? throw new UnauthorizedAccessException("User is not authenticated.");
            await wishlistService.AddToWishlistAsync(customer, productId);
            return NoContent();
        }

        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFromWishlistAsync(string productId)
        {
            var customer = userContextService.GetUser() 
                         ?? throw new UnauthorizedAccessException("User is not authenticated.");
            await wishlistService.RemoveFromWishlistAsync(customer, productId);
            return NoContent();
        }

    }
}
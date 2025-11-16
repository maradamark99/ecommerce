using Ecommerce.Domain.Auth;
using Ecommerce.Domain.Auth.Contract;
using Ecommerce.Domain.Wishlist.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Domain.Wishlist
{
    [ApiController]
    [Route("api/v{version:apiVersion}/wishlist")]
    public class WishlistController(
        IWishlistService wishlistService, 
        IUserContextService userContextService) : ControllerBase
    {

        [Authorize(Roles = nameof(Roles.Customer))]
        [HttpGet]
        public async Task<IActionResult> GetWishlist()
        {
            var userId = userContextService.GetUserId(); 
            var wishlist = await wishlistService.GetWishlistAsync(userId!);
            return Ok(wishlist);
        }
        
        [Authorize(Roles = nameof(Roles.Customer))]
        [HttpPost]
        [Route("{productId}")]
        public async Task<IActionResult> AddToWishlist([FromRoute] string productId)
        {
            var userId = userContextService.GetUserId();
            await wishlistService.AddToWishlistAsync(userId!, productId);
            return NoContent();
        }

        [Authorize(Roles = nameof(Roles.Customer))]
        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFromWishlist(string productId)
        {
            var userId = userContextService.GetUserId();
            await wishlistService.RemoveFromWishlistAsync(userId!, productId);
            return NoContent();
        }

    }
}
using Ecommerce.Common.Data;
using Ecommerce.Common.Exception;
using Ecommerce.Domain.Auth;
using Ecommerce.Domain.ProductManagement;
using Ecommerce.Domain.ProductManagement.ProductCatalog.Contract;
using Ecommerce.Domain.Wishlist.Contract;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Domain.Wishlist;

public class WishlistService(
    IWishlistRepository wishlistRepository,
    IProductCatalogService productCatalogService,
    UserManager<AppUser> userManager) : IWishlistService
{
    
    public async Task<IEnumerable<WishlistItemDto>?> GetWishlistAsync(string userId)
    {
        var wishlist = await wishlistRepository.GetWishlistAsync(userId);
        return wishlist?.Items.Select(item => new WishlistItemDto
        {
            ProductId = item.Product.Id.ToString(),
            ProductName = item.Product.Name,
            AddedAt = item.AddedAt
        });
    }

    public async Task AddToWishlistAsync(string customerId, string productId)
    {
        var customer = await userManager.FindByIdAsync(customerId);
        var productCatalogResponse = await productCatalogService.GetProductByIdAsync(productId);
        if (productCatalogResponse == null)
            throw new NotFoundException("Product not found");
        var wishlist = await wishlistRepository.GetWishlistAsync(customerId);
        if (wishlist == null)
        {
            await wishlistRepository.CreateWishlistAsync(customer!, productId);
        }
        else
        {
            await wishlistRepository.AddToWishlistAsync(wishlist, productId);  
        }
    }

    public async Task RemoveFromWishlistAsync(string customerId, string productId)
    {
        var wishList = await wishlistRepository.GetWishlistAsync(customerId);
        if (wishList == null)
            throw new NotFoundException("Wishlist not found");
        await wishlistRepository.RemoveFromWishlistAsync(wishList, productId);
    }

}
using Ecommerce.Common.Data;
using Ecommerce.Domain.Auth;
using Ecommerce.Domain.ProductManagement;
using Ecommerce.Domain.Wishlist.Contract;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Domain.Wishlist;

public class WishlistRepository(AppDbContext dbContext) : IWishlistRepository
{
    public async Task<Wishlist?> GetWishlistAsync(string customerId)
    {
        return await dbContext.Wishlists
            .Include(w => w.Customer)
            .Include(w => w.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(w => w.Customer.Id == customerId);
    }

    public Task CreateWishlistAsync(AppUser customer, string productId)
    {
        var wishlist = new Wishlist
        {
            Id = Guid.NewGuid(),
            Customer = customer,
            Items = new List<WishlistItem>(),
            UpdatedAt = DateTime.UtcNow
        };

        wishlist.Items.Add(new WishlistItem
        {
            ProductId = Guid.Parse(productId),
            AddedAt = DateTime.UtcNow
        });

        dbContext.Wishlists.Add(wishlist);
        return dbContext.SaveChangesAsync();
    }

    public Task AddToWishlistAsync(Wishlist wishlist, string productId)
    {
        var now = DateTime.UtcNow;
        wishlist.UpdatedAt = now;
        wishlist.Items.Add(new WishlistItem
        {
            ProductId = Guid.Parse(productId),
            AddedAt = now
        });

        return dbContext.SaveChangesAsync();
    }
    
    public async Task RemoveFromWishlistAsync(Wishlist wishlist, string productId)
    {
        var item = wishlist.Items.FirstOrDefault(i => i.Product.Id.ToString() == productId);
        if (item == null) return; 

        wishlist.Items.Remove(item);
        wishlist.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
    }

}
using Microsoft.EntityFrameworkCore;
using Wishlist.Contract;

namespace Wishlist;

public class WishlistRepository(AppDbContext dbContext) : IWishlistRepository
{

    public async Task<Wishlist?> GetWishlistForCustomerAsync(string customerId)
    {
        return await dbContext.Wishlists
            .Include(w => w.Products)
            .Where(w => w.Products.Any(p => p.IsListed))
            .FirstOrDefaultAsync(w => w.CustomerId == customerId);
    }

    public Task<List<string>> GetCustomersWhoHaveWishlistedTheProductAsync(string productId)
    {
        return dbContext.Wishlists
            .Where(w => w.Products.Any(p => p.Id == productId))
            .Select(w => w.CustomerId)
            .ToListAsync();
    }

    public Task CreateWishlistAsync(string customerId, Product product)
    {
        var wishlist = new Wishlist
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = customerId,
            Products = new List<Product>(),
            UpdatedAt = DateTime.UtcNow
        };

        wishlist.Products.Add(product);
        dbContext.Wishlists.Add(wishlist);
        return dbContext.SaveChangesAsync();
    }

    public Task AddToWishlistAsync(Wishlist wishlist, Product product)
    {
        wishlist.UpdatedAt = DateTime.UtcNow;
        wishlist.Products.Add(product);

        return dbContext.SaveChangesAsync();
    }

    public async Task RemoveFromWishlistAsync(Wishlist wishlist, string productId)
    {
        var item = wishlist.Products.FirstOrDefault(i => i.Id == productId);
        if (item == null) return; 

        wishlist.Products.Remove(item);
        wishlist.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
    }

    public Task UpdateProductAsync(Product product)
    {
        dbContext.Products.Update(product);
        return dbContext.SaveChangesAsync();
    }

    public Task<Product?> GetProductByIdAsync(string productId)
    {
        return dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId);
    }

    public async Task UpdateProductListingAsync(string productId, bool isListed)
    {
        await dbContext.Products.Where(r => r.Id == productId)
            .ExecuteUpdateAsync(r => r.SetProperty(x => x.IsListed, isListed));
    }
}
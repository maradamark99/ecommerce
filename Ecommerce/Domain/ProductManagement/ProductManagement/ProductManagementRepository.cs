using Ecommerce.Common.Data;
using Ecommerce.Common.Pagination;
using Ecommerce.Domain.ProductManagement.ProductManagement.Contract;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Domain.ProductManagement.ProductManagement;

public class ProductManagementRepository(AppDbContext dbContext) : IProductManagementRepository, IProductMediaRepository, IProductListingRepository
{
    public async Task<Paged<Product>> GetAllAsync(Pager pager, Sorter sorter)
    {
        long totalItems = await dbContext.Products.CountAsync();

        var productsQuery = dbContext.Products
            .Include(p => p.Media)
            .Include(p => p.Attributes)
            .Include(p => p.Category)
            .Skip(pager.Offset)
            .Take(pager.PageSize);
            
        productsQuery = sorter.SortOrder == SortOrder.Ascending 
            ? productsQuery.OrderBy(p => EF.Property<Product>(p, sorter.SortBy)) 
            : productsQuery.OrderByDescending(p => EF.Property<Product>(p, sorter.SortBy));

        return Paged<Product>.Of(await productsQuery.ToListAsync(), pager.PageNumber, pager.PageSize, totalItems);
    }

    public async Task<Product?> GetByIdAsync(string id)
    {
        return await dbContext.Products
            .Include(p => p.Media)
            .Include(p => p.Attributes)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id.ToString() == id);
    }

    public async Task<string> CreateAsync(Product product)
    {
        await dbContext.Products.AddAsync(product);
        await dbContext.SaveChangesAsync();
        return product.Id.ToString();
    }

    public Task<bool> ExistsByIdAsync(string id)
    {
        return dbContext.Set<Product>().AnyAsync(p => p.Id.ToString() == id);
    }

    public async Task DeleteByIdAsync(string id)
    {
        var product = await dbContext.Set<Product>().FindAsync(id);
        if (product != null)
        {
            dbContext.Set<Product>().Remove(product);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task UpdateAsync(string id, Product updatedProduct)
    {
        var existingProduct = await dbContext.Set<Product>().FindAsync(id);
        if (existingProduct != null)
        {
            existingProduct.Name = updatedProduct.Name;
            existingProduct.Description = updatedProduct.Description;
            existingProduct.ProductCondition = updatedProduct.ProductCondition;
            existingProduct.Category = updatedProduct.Category;
            dbContext.Set<Product>().Update(existingProduct);

            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<ProductMedia?> GetProductMediaByIdAsync(string productId, long mediaId)
    {
        var product = await dbContext.Products.Include(p => p.Media).FirstOrDefaultAsync(p => p.Id.ToString() == productId);
        return product?.Media?.FirstOrDefault(m => m.Id == mediaId);
    }

    public async Task AddMediaAsync(string productId, ProductMedia media)
    {
        var product = await dbContext.Products
            .Include(p => p.Media)
            .FirstOrDefaultAsync(p => p.Id.ToString() == productId);

        if (product != null)
        {
            product.Media?.Add(media);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task RemoveMediaAsync(string productId, long mediaId)
    {
        var product = await dbContext.Products
            .Include(p => p.Media)
            .FirstOrDefaultAsync(p => p.Id.ToString() == productId);

        var mediaToRemove = product?.Media?.FirstOrDefault(m => m.Id == mediaId);
        if (product != null && mediaToRemove != null)
        {
            product.Media!.Remove(mediaToRemove);
            await dbContext.SaveChangesAsync();
        }
    }
    
    public async Task<List<ProductListing>?> GetProductListingHistoryAsync(string productId)
    {
        var product = await dbContext.Products
            .Include(p => p.ProductListings)
            .FirstOrDefaultAsync(p => p.Id.ToString() == productId);
        return product?.ProductListings?.ToList();
    }

    public async Task ListProductAsync(string productId, ProductListing productListing)
    {
        var product = await dbContext.Products
            .Include(p => p.ProductListings)
            .FirstOrDefaultAsync(p => p.Id.ToString() == productId);
        product!.ProductListings!.Add(productListing);
        await dbContext.SaveChangesAsync();
    }

    public async Task<bool> IsProductAlreadyListedAsync(string productId)
    {
        return await dbContext.Products
            .AnyAsync(p => 
                p.Id.ToString() == productId && 
                p.ProductListings != null && 
                p.ProductListings.Any(l => l.IsActive)
            );
    }

    public async Task DelistProductAsync(string productId)
    {
        var product = await dbContext.Products
            .Include(p => p.ProductListings)
            .FirstOrDefaultAsync(p => p.Id.ToString() == productId);
        product!.ProductListings!
            .Where(l => l.IsActive)
            .ToList()
            .ForEach(l => l.IsActive = false);
        await dbContext.SaveChangesAsync();
    }

}

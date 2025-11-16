using System.Linq.Expressions;
using EcommerceLib.Exception;
using EcommerceLib.Pagination;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Contract;
using ProductManagement.Model;
using Attribute = ProductManagement.Model.Attribute;

namespace ProductManagement;

public class ProductManagementRepository(AppDbContext dbContext) : IProductManagementRepository, IProductMediaRepository, IProductListingRepository
{
    

    public async Task<Paged<Product>> GetAllAsync(Pager pager, Sorter sorter)
    {
        long totalItems = await dbContext.Products.CountAsync();

        var productsQuery = dbContext.Products
            .Include(p => p.Media)
            .Include(p => p.Attributes)
            .Include(p => p.Category);

        if (!SortSelectors.TryGetValue(sorter.SortBy, out var selector))
        {
            selector = SortSelectors["id"];
        }

        var orderedAndPaginated = (sorter.SortOrder == SortOrder.Ascending
            ? productsQuery.OrderBy(selector)
            : productsQuery.OrderByDescending(selector))
                .Skip(pager.Offset)
                .Take(pager.PageSize);

        var items = await orderedAndPaginated.ToListAsync();

        return Paged<Product>.Of(items, pager.PageNumber, pager.PageSize, totalItems);
    }

    public async Task<Product?> GetByIdAsync(string id)
    {
        return await dbContext.Products
            .Include(p => p.Media)
            .Include(p => p.Attributes)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<string> CreateAsync(Product product)
    {
        await dbContext.Products.AddAsync(product);
        await dbContext.SaveChangesAsync();
        return product.Id.ToString();
    }

    public Task<bool> ExistsByIdAsync(string id)
    {
        return dbContext.Set<Product>().AnyAsync(p => p.Id == id);
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
        var existingProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (existingProduct != null)
        {
            existingProduct.Name = updatedProduct.Name;
            existingProduct.Description = updatedProduct.Description;
            existingProduct.Condition = updatedProduct.Condition;
            existingProduct.Category = updatedProduct.Category;
            existingProduct.Attributes = updatedProduct.Attributes;
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<ProductMedia?> GetProductMediaByIdAsync(string productId, long mediaId)
    {
        var product = await dbContext.Products.Include(p => p.Media).FirstOrDefaultAsync(p => p.Id == productId);
        return product?.Media?.FirstOrDefault(m => m.Id == mediaId);
    }

    public async Task AddMediaAsync(string productId, ProductMedia media)
    {
        var product = await dbContext.Products
            .Include(p => p.Media)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product != null)
        {
            if (product.Media == null)
            {
                product.Media = [];
            }
            product.Media?.Add(media);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task RemoveMediaAsync(string productId, long mediaId)
    {
        var product = await dbContext.Products
            .Include(p => p.Media)
            .FirstOrDefaultAsync(p => p.Id == productId);

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
            .FirstOrDefaultAsync(p => p.Id == productId);
        return product?.ProductListings?.ToList();
    }

    public async Task ListProductAsync(string productId, ProductListing productListing)
    {
        var product = await dbContext.Products
            .Include(p => p.ProductListings)
            .FirstOrDefaultAsync(p => p.Id == productId);
        product!.ProductListings!.Add(productListing);
        await dbContext.SaveChangesAsync();
    }

    public async Task<bool> IsProductAlreadyListedAsync(string productId)
    {
        return await dbContext.Products
            .AnyAsync(p => 
                p.Id == productId && 
                p.ProductListings != null && 
                p.ProductListings.Any(l => l.IsActive)
            );
    }

    public async Task DelistProductAsync(string productId)
    {
        var product = await dbContext.Products
            .Include(p => p.ProductListings)
            .FirstOrDefaultAsync(p => p.Id == productId);
        product!.ProductListings!
            .Where(l => l.IsActive)
            .ToList()
            .ForEach(l => l.IsActive = false);
        await dbContext.SaveChangesAsync();
    }
    
    public async Task<ProductListing?> GetActiveProductListingAsync(string productId) {
        return await dbContext.ProductListings
            .Include(p => p.Product)
            .FirstOrDefaultAsync(p => p.Product.Id == productId && p.IsActive); 
    }

    public Task UpdateProductListingAsync(ProductListing listing)
    {
        dbContext.ProductListings.Update(listing);
        return dbContext.SaveChangesAsync();
    }
    
    public async Task<List<Attribute>> GetExistingAttributesAsync(List<Attribute> attributes)
    {
        var names = attributes.Select(a => a.Name).Distinct().ToList();

        var potentialMatches = await dbContext.ProductAttributes
            .Where(a => names.Contains(a.Name))
            .ToListAsync();

        var existing = potentialMatches
            .Where(dbAttr => attributes
                .Any(reqAttr => dbAttr.Name == reqAttr.Name && dbAttr.Value == reqAttr.Value))
            .ToList();

        return existing;
    }
    
    private static readonly Dictionary<string, Expression<Func<Product, object>>> SortSelectors =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = p => p.Id,
            ["name"] = p => p.Name,
            ["condition"] = p => p.Condition
        };
}

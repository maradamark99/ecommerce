using Ecommerce.Common.Data;
using Ecommerce.Common.Pagination;
using Ecommerce.Domain.ProductManagement.ProductCatalog.Contract;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Domain.ProductManagement.ProductCatalog;

public class ProductCatalogRepository(AppDbContext dbContext) : IProductCatalogRepository
{
    public async Task<Paged<Product>> GetProductsAsync(ProductFilter filter, Pager pager, Sorter sorter)
    {
        var query = dbContext.Products.AsQueryable()
            .Where(p => p.ProductListings != null && p.ProductListings.Any(l => l.IsActive));
        
        if (filter.CategoryId.HasValue)
        {
            query = query.Where(p => p.Category.Id == filter.CategoryId);
        }
        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => p.ProductListings!.First(l => l.IsActive).PriceInEur >= filter.MinPrice.Value);
        }
        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => p.ProductListings!.First(l => l.IsActive).PriceInEur <= filter.MaxPrice.Value);
        }
        /*if (!string.IsNullOrEmpty(filter.Brand))
        {
            query = query.Where(p => p.Brand == filter.Brand);
        }*/
        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            query = query.Where(p => p.Name.ToLower().Contains(filter.SearchTerm.ToLower()));
        }

        if (filter.AttributeFilters != null && filter.AttributeFilters.Any())
        {
            query = filter
                .AttributeFilters
                .Aggregate(query, 
                    (current, attributeFilter) => current
                        .Where(p => p.Attributes.Any(a => a.Name == attributeFilter.Name && a.Value == attributeFilter.Value)
                        )
                    );
        }

        if (!string.IsNullOrEmpty(sorter.SortBy))
        {
            query = sorter.SortOrder == SortOrder.Ascending
                ? query.OrderBy(p => EF.Property<object>(p, sorter.SortBy))
                : query.OrderByDescending(p => EF.Property<object>(p, sorter.SortBy));
        }

        var totalCount = await query.CountAsync();
        var products = await query.Skip(pager.Offset)
                                            .Take(pager.PageSize)
                                            .Select(p => new Product
                                                {
                                                    Id = p.Id,
                                                    Name = p.Name,
                                                    Description = p.Description,
                                                    Category = p.Category,
                                                    Media = p.Media,
                                                    Attributes = p.Attributes,
                                                    ProductListings = p.ProductListings!
                                                        .Where(l => l.IsActive)
                                                        .ToList()
                                                })
                                            .ToListAsync();

        return Paged<Product>.Of(products, pager.PageNumber, pager.PageSize, totalCount);
    }

    public async Task<Product?> GetProductByIdAsync(string id)
    {
        return await dbContext.Products
            .Where(p => p.Id.ToString() == id && p.ProductListings != null && p.ProductListings.Any(l => l.IsActive))
            .Include(p => p.Category)
            .Include(p => p.Media)
            .Include(p => p.Attributes)
            .Select(p => new Product
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Category = p.Category,
                Media = p.Media,
                Attributes = p.Attributes,
                ProductListings = p.ProductListings!
                    .Where(l => l.IsActive)
                    .Take(1)
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }
    
}
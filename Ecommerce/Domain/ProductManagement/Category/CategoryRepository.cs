using Ecommerce.Common.Data;
using Ecommerce.Common.Pagination;
using Ecommerce.Domain.ProductManagement.Category.Contract;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Domain.ProductManagement.Category;

public class CategoryRepository(AppDbContext dbContext) : ICategoryRepository
{
    public async Task<Paged<Category>> GetAllCategoriesAsync(Pager pager, Sorter sorter)
    {
        long totalItems = await dbContext.Categories.CountAsync();

        var categoriesQuery = dbContext.Categories
            .Skip(pager.Offset)
            .Take(pager.PageSize);
        
        categoriesQuery = sorter.SortOrder == SortOrder.Ascending 
            ? categoriesQuery.OrderBy(c => EF.Property<Category>(c, sorter.SortBy)) 
            : categoriesQuery.OrderByDescending(c => EF.Property<Category>(c, sorter.SortBy));

        return Paged<Category>.Of(await categoriesQuery.ToListAsync(), pager.PageNumber, pager.PageSize, totalItems);
    }

    public async Task<long> CreateCategoryAsync(Category category)
    {   
        var result = await dbContext.Categories.AddAsync(category);
        await dbContext.SaveChangesAsync();
        return result.Entity.Id;
    }

    public async Task<Category?> GetByIdAsync(long id)
    {
        var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category != null)
        {
            var attrDefinitions = await GetCategoryAttributeDefinitionsAsync(category.Path);
            category.AttributesDefinitions = attrDefinitions?.ToList();
        }
        return category;
    }

    public async Task<IEnumerable<AttributeDefinition>?> GetCategoryAttributeDefinitionsAsync(string path)
    { 
        return await GetAttributeDefinitionsTillRoot(path);
    }

    private async Task<List<AttributeDefinition>> GetAttributeDefinitionsTillRoot(string path)
    {
        var attributeDefinitions = await dbContext.Categories
            .Where(c => path.StartsWith(c.Path) && c.AttributesDefinitions != null) 
            .SelectMany(c => c.AttributesDefinitions!)
            .Distinct()
            .ToListAsync();

        return attributeDefinitions;
    }

    public Task<bool> ExistsById(long id)
    {
        return dbContext.Categories.AnyAsync(x => x.Id == id);
    }

    public Task<bool> ExistsByPath(string path)
    {
        return dbContext.Categories.AnyAsync(x => x.Path == path);
    }

    public async Task DeleteCategoryAsync(long id)
    {
        var category = await dbContext.Set<Category>().FindAsync(id);
        if (category != null)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            try 
            {
                var children = await dbContext.Categories
                    .Where(c => c.Path.StartsWith(category.Path + "/"))
                    .ToListAsync();
                
                dbContext.Set<Category>().RemoveRange(children);
                dbContext.Set<Category>().Remove(category);
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    public async Task UpdateCategoryAsync(long id, Category categoryToUpdate)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var existingCategory = await dbContext.Categories
            .Include(c => c.AttributesDefinitions)
            .FirstOrDefaultAsync(c => c.Id == id);
            if (existingCategory != null)
            {
                var oldPath = existingCategory.Path;
                var newPath = categoryToUpdate.Path;
    
                existingCategory.PublicName = categoryToUpdate.PublicName;
                existingCategory.Slug = categoryToUpdate.Slug;
                existingCategory.AttributesDefinitions = categoryToUpdate.AttributesDefinitions; 
                existingCategory.Path = newPath;

                dbContext.Categories.Update(existingCategory);

                if (oldPath != newPath)
                {
                    var children = await dbContext.Categories
                        .Where(c => c.Path.StartsWith(oldPath + "/"))
                        .ToListAsync();
    
                    foreach (var child in children)
                    {
                        child.Path = newPath + child.Path[oldPath.Length..];
                        dbContext.Categories.Update(child);
                    }
                }
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
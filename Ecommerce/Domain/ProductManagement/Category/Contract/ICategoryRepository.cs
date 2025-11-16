using Ecommerce.Common.Pagination;

namespace Ecommerce.Domain.ProductManagement.Category.Contract;

public interface ICategoryRepository
{
    
    public Task<Paged<Category>> GetAllCategoriesAsync(Pager pager, Sorter sorter);    
    
    public Task<long> CreateCategoryAsync(Category category);
    
    public Task<Category?> GetByIdAsync(long id);
    
    public Task<IEnumerable<AttributeDefinition>?> GetCategoryAttributeDefinitionsAsync(string path);
    
    public  Task<bool> ExistsById(long id);
    public Task<bool> ExistsByPath(string path);
    
    public Task DeleteCategoryAsync(long id);

    public Task UpdateCategoryAsync(long id, Category categoryToUpdate);
    
}
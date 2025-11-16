using Ecommerce.Common.Pagination;

namespace Ecommerce.Domain.ProductManagement.Category.Contract;

public interface ICategoryService
{ 
    
    Task<Paged<CategoryResponse>> GetAllCategoriesAsync(Pager pager, Sorter sorter);
    
    Task<long> CreateCategoryAsync(CategoryRequest categoryRequest);

    Task<CategoryResponse> GetCategoryByIdAsync(long id);

    Task<IEnumerable<AttributeDefinition>?> GetAttributeDefinitionsForCategoryAsync(long categoryId);
    
    Task DeleteCategoryAsync(long id);

    Task UpdateCategoryAsync(long id, CategoryRequest categoryToUpdate);
    
}
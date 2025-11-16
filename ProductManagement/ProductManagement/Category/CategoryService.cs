using EcommerceLib.Exception;
using EcommerceLib.Pagination;
using ProductManagement.Category.Contract;

namespace ProductManagement.Category;

public class CategoryService(
    ICategoryMapper categoryMapper,
    ICategoryRepository categoryRepository)
    : ICategoryService
{
    public async Task<Paged<CategoryResponse>> GetAllCategoriesAsync(Pager pager, Sorter sorter)
    {
        if (!Category.SortableColumns.Contains(sorter.SortBy))
        {
            throw new BadRequestException($"Invalid column name for sorting: {sorter.SortBy}");
        }
        
        var categories = await categoryRepository.GetAllCategoriesAsync(pager, sorter);
        return categories.Select(categoryMapper.ModelToResponse);
    }
    
    public async Task<CategoryResponse> GetCategoryByIdAsync(long id)
    {
        var category = await DoGetCategoryByIdAsync(id);
        return categoryMapper.ModelToResponse(category);
    }

    public async Task<long> CreateCategoryAsync(CategoryRequest categoryRequest)
    {
        Category? parent = null;
        if (categoryRequest.ParentId != null)
        {
            parent = await DoGetCategoryByIdAsync(categoryRequest.ParentId ?? 0);
        }
        var categoryToSave = categoryMapper.RequestToModel(categoryRequest, parent);
        if (await categoryRepository.ExistsByPath(categoryToSave.Path))
        {
            throw new ConflictException(
                $"Category already exists with path: {categoryToSave.Path}");
        }
        
        var createdId = await categoryRepository.CreateCategoryAsync(categoryToSave);
        return createdId;
    }

    public async Task DeleteCategoryAsync(long id)
    {
        if (!await categoryRepository.ExistsById(id))
        {
            throw new NotFoundException($"Category with id: {id} not found");
        }
        await categoryRepository.DeleteCategoryAsync(id);
    }

    public async Task UpdateCategoryAsync(long id, CategoryRequest categoryToUpdate)
    {
        var oldCategory = await DoGetCategoryByIdAsync(id);
        var parent = categoryToUpdate.ParentId != null
            ? await DoGetCategoryByIdAsync(categoryToUpdate.ParentId.Value)
            : null;
        
        var newCategory = categoryMapper.RequestToModel(categoryToUpdate, parent);
        
        var doesCategoryExistByPath = await categoryRepository.ExistsByPath(newCategory.Path);
        var isSomethingChanged = oldCategory.Path != newCategory.Path;

        if (!isSomethingChanged) 
        {
            return;
        }
        if (isSomethingChanged && doesCategoryExistByPath)
        {
            throw new ConflictException(
                $"Category already exists with path: {newCategory.Path}");
        }

        await categoryRepository.UpdateCategoryAsync(id, newCategory);
    }

    public async Task<IEnumerable<AttributeDefinition>?> GetAttributeDefinitionsForCategoryAsync(long categoryId)
    {
        var category = await categoryRepository.GetByIdAsync(categoryId);
        if (category == null)
        {
            throw new NotFoundException($"Category with id: {categoryId} not found");
        }
        return await categoryRepository.GetCategoryAttributeDefinitionsAsync(category.Path);
    }
    
    private async Task<Category> DoGetCategoryByIdAsync(long id)
    {
        var category = await categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            throw new NotFoundException($"Category with id: {id} not found");
        }
        return category;
    }
    
}
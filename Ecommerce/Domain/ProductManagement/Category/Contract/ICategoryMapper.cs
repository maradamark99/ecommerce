namespace Ecommerce.Domain.ProductManagement.Category.Contract;

public interface ICategoryMapper
{   
    
    Category RequestToModel(CategoryRequest request, Category? parent);

    CategoryResponse ModelToResponse(Category model);
    
}
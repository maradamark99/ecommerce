namespace ProductManagement.Category.Contract;

public interface ICategoryMapper
{   
    
    global::ProductManagement.Category.Category RequestToModel(CategoryRequest request, global::ProductManagement.Category.Category? parent);

    CategoryResponse ModelToResponse(global::ProductManagement.Category.Category model);
    
}
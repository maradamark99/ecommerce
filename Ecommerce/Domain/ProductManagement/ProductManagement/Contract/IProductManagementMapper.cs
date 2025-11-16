namespace Ecommerce.Domain.ProductManagement.ProductManagement.Contract;

public interface IProductManagementMapper
{
    Product RequestToModel(CreateProductRequestDto createProductRequestDto, Category.Category category);
    
    ProductManagementResponse ModelToResponse(Product product);    
}
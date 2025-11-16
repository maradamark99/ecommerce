using ProductManagement.Model;
using Attribute = ProductManagement.Model.Attribute;

namespace ProductManagement.Contract;

public interface IProductManagementMapper
{
    Product RequestToModel(CreateProductRequestDto createProductRequestDto, Category.Category category, List<Attribute> attributes);
    
    ProductManagementResponse ModelToResponse(Product product);    
}
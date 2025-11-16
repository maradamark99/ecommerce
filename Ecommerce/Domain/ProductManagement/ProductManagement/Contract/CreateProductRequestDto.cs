using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Domain.ProductManagement.ProductManagement.Contract;

public record CreateProductRequestDto(
    [Required(ErrorMessage = "Name is required")] string Name,
    [Required(ErrorMessage = "Description is required")] string Description,
    long CategoryId, 
    List<ProductAttributeRequest> Attributes, 
    ProductCondition ProductCondition
);
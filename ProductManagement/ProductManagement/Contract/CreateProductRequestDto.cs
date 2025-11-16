using System.ComponentModel.DataAnnotations;
using ProductManagement.Model;

namespace ProductManagement.Contract;

public record CreateProductRequestDto(
    [Required(ErrorMessage = "Name is required")] string Name,
    [Required(ErrorMessage = "Description is required")] string Description,
    long CategoryId, 
    List<ProductAttributeRequest> Attributes, 
    string ProductCondition
);
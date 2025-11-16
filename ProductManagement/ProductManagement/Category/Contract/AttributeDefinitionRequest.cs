using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Category.Contract;

public record AttributeDefinitionRequest(
    [Required(ErrorMessage = "Name is required")] string Name, 
    string Type, 
    bool IsRequired
);
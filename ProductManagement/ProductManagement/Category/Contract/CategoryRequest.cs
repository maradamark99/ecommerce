using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Category.Contract;

public record CategoryRequest(
    [Required(ErrorMessage = "Name is required")] string Name,
    long? ParentId,
    IEnumerable<AttributeDefinitionRequest>? AttributeDefinitions
);
using EcommerceLib;
using ProductManagement.Category;
using ProductManagement.Model;
using Attribute = ProductManagement.Model.Attribute;

namespace ProductManagement.Contract;

public interface IProductAttributesValidator
{
    ValidationResult Validate(IEnumerable<Attribute> attributes, IEnumerable<AttributeDefinition>? attributeDefinitions);
}
using Ecommerce.Common;
using Ecommerce.Domain.ProductManagement.Category;

namespace Ecommerce.Domain.ProductManagement.ProductManagement.Contract;

public interface IProductAttributesValidator
{
    ValidationResult Validate(IEnumerable<ProductAttribute> attributes, IEnumerable<AttributeDefinition>? attributeDefinitions);
}
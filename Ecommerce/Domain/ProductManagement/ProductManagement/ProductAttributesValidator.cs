using System.Text;
using Ecommerce.Common;
using Ecommerce.Domain.ProductManagement.Category;
using Ecommerce.Domain.ProductManagement.ProductManagement.Contract;

namespace Ecommerce.Domain.ProductManagement.ProductManagement;

public class ProductAttributesValidator : IProductAttributesValidator
{
    public ValidationResult Validate(IEnumerable<ProductAttribute> attributes, IEnumerable<AttributeDefinition>? attributeDefinitions)
    {
        var result = new ValidationResult
        {
            IsValid = true
        };
        var attributesDict = attributes.ToDictionary((attribute) => attribute.Name, (attribute) => attribute.Value);
        var message = new StringBuilder();
        foreach (var attrDef in attributeDefinitions)
        {
            if (!attributesDict.TryGetValue(attrDef.Name, out var attrValue))
            {
                result.IsValid = false;
                message.Append($"Attribute {attrDef.Name} is not present, but required\n");
                continue;
            }
            if (!TryValidateType(attrValue, attrDef.Type))
            {
                result.IsValid = false;
                message.Append($"Attribute {attrDef.Name} is not of expected type, expected type: {attrDef.Type}\n");
            }
        }
        result.ErrorMessage = message.ToString();
        return result;
    }


    private bool TryValidateType(string value, AttributeType expectedType)
    {
        return expectedType switch
        {
            AttributeType.NUMERIC => double.TryParse(value, out _),
            AttributeType.STRING => true,
            AttributeType.BOOL => bool.TryParse(value, out _),
            _ => false
        };
    }
}
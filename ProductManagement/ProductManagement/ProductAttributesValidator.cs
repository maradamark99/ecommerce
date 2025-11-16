using System.Text;
using EcommerceLib;
using ProductManagement.Category;
using ProductManagement.Contract;
using ProductManagement.Model;
using Attribute = ProductManagement.Model.Attribute;

namespace ProductManagement;

public class ProductAttributesValidator : IProductAttributesValidator
{
    public ValidationResult Validate(IEnumerable<Attribute> attributes, IEnumerable<AttributeDefinition>? attributeDefinitions)
    {
        var result = new ValidationResult
        {
            IsValid = true
        };
        var attributesDict = attributes.ToDictionary((attribute) => attribute.Name, (attribute) => attribute.Value);
        var message = new StringBuilder();
        foreach (var attrDef in attributeDefinitions ?? [])
        {
            if (!attributesDict.TryGetValue(attrDef.Name, out var attrValue))
            {
                if (attrDef.IsRequired)
                {
                    result.IsValid = false;
                    message.Append($"Attribute {attrDef.Name} is not present, but required\n");
                }
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
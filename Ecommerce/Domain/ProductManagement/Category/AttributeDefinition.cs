namespace Ecommerce.Domain.ProductManagement.Category;

public class AttributeDefinition
{
    public long Id { get; set; }
    public string Name { get; set; }
    
    public AttributeType Type { get; set; }
    public bool IsRequired { get; set; }
    
    public Category Category { get; set; }
    
    public static AttributeDefinitionBuilder Builder() => new AttributeDefinitionBuilder();

    public class AttributeDefinitionBuilder
    {
        private readonly AttributeDefinition _attributeDefinition;

        public AttributeDefinitionBuilder()
        {
            _attributeDefinition = new AttributeDefinition();
        }

        public AttributeDefinitionBuilder WithId(long id)
        {
            _attributeDefinition.Id = id;
            return this;
        }

        public AttributeDefinitionBuilder WithName(string name)
        {
            _attributeDefinition.Name = name;
            return this;
        }

        public AttributeDefinitionBuilder WithType(AttributeType type)
        {
            _attributeDefinition.Type = type;
            return this;
        }

        public AttributeDefinitionBuilder WithIsRequired(bool isRequired)
        {
            _attributeDefinition.IsRequired = isRequired;
            return this;
        }

        public AttributeDefinition Build()
        {
            return _attributeDefinition;
        }
    }
}


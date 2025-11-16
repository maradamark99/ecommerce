namespace Ecommerce.Domain.ProductManagement;

public class ProductAttribute
{
    public long Id { get; set; }
    public string Name { get; set; }
    
    public string Value { get; set; }
    
    public Product Product { get; set; }
    
    private ProductAttribute(long id, string name, string value)
    {
        Id = id;
        Name = name;
        Value = value;
    }

    public static AttributeBuilder Builder() => new AttributeBuilder();

    public class AttributeBuilder
    {
        private long _id;
        private string _name;
        private string _value;

        public AttributeBuilder WithId(long id)
        {
            _id = id;
            return this;
        }

        public AttributeBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public AttributeBuilder WithValue(string value)
        {
            _value = value;
            return this;
        }

        public ProductAttribute Build()
        {
            return new ProductAttribute(_id, _name, _value);
        }
    }
}
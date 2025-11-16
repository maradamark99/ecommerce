namespace ProductManagement.ProductCatalog;

public class AttributeFilter
{ 
    
    public string Name { get; set; }
    
    public string Value { get; set; }

    public AttributeFilter(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public AttributeFilter()
    {
        
    }
    
}
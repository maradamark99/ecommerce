using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductManagement.Model;

public class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    
    public bool IsInStock { get; set; }
    
    public string Description { get; set; }
    
    public Condition Condition { get; set; }
    
    public ProductManagement.Category.Category Category { get; set; }
    
    public List<ProductListing>? ProductListings { get; set; }
    
    public List<Attribute> Attributes { get; set; } = [];
    
    public List<ProductMedia>? Media { get; set; }
    
    public Product(string name, string description, Condition condition, ProductManagement.Category.Category category, List<Attribute> attributes)
    {
        Name = name;
        Condition = condition;
        Description = description;
        Category = category;
        Attributes = attributes;
    }

    public Product()
    {
        
    }
    
    
}
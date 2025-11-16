using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce.Domain.ProductManagement;

public class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    
    public string Description { get; set; }
    
    public ProductCondition ProductCondition { get; set; }
    
    public Category.Category Category { get; set; }
    
    public List<ProductListing>? ProductListings { get; set; }
    
    public List<ProductAttribute> Attributes { get; set; }
    
    public List<ProductMedia>? Media { get; set; }
    
    public static readonly string[] SortableColumns = ["id", "name", "condition"];

    public Product(string name, string description, ProductCondition productCondition, Category.Category category, List<ProductAttribute> attributes)
    {
        Name = name;
        ProductCondition = productCondition;
        Description = description;
        Category = category;
        Attributes = attributes;
    }

    public Product()
    {
        
    }
    
    
}
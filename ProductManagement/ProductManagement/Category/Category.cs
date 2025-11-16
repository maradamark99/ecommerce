using Microsoft.EntityFrameworkCore;
using ProductManagement.Model;

namespace ProductManagement.Category;

[Index(nameof(Path))]
public class Category
{
    public long Id { get; set; }
    
    public string PublicName { get; set; }
    
    public string Slug { get; set; }
    public string Path { get; set; }
    
    public List<AttributeDefinition>? AttributesDefinitions { get; set; } 
    
    public List<Product>? Products { get; set; }
    
    
    public static readonly string[] SortableColumns = [nameof(Id), nameof(PublicName), nameof(Path)];

    public static CategoryBuilder Builder() => new CategoryBuilder();
  
    public class CategoryBuilder
    {
        private readonly Category _category = new();
        
        public CategoryBuilder WithId(long id)
        {
            _category.Id = id;
            return this;
        }

        public CategoryBuilder WithPublicName(string name)
        {
            _category.PublicName = name;
            return this;
        }
        
        public CategoryBuilder WithSlug(string slug)
        {
            _category.Slug = slug;
            return this;
        }
        
        public CategoryBuilder WithPath(string path)
        {
            _category.Path = path;
            return this;
        }

        public CategoryBuilder WithAttributeDefinitions(List<AttributeDefinition>? attributes)
        {
            _category.AttributesDefinitions = attributes;
            return this;
        }
        
        public Category Build()
        {
            return _category;
        }
    }
    
}



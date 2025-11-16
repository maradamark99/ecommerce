namespace ProductManagement.ProductCatalog;

public class ProductFilter
{
    
    public long? CategoryId { get; set; }
    
    public decimal? MinPrice { get; set; }
    
    public decimal? MaxPrice { get; set; }
    
    public string? Brand { get; set; }
    
    public string? SearchTerm { get; set; }
    
    public List<AttributeFilter>? AttributeFilters { get; set; }
    
}
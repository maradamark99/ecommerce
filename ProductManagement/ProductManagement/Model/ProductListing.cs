namespace ProductManagement.Model;

public class ProductListing
{
    
    public long Id { get; set; }
    
    public decimal Price { get; set; } 
    
    public bool IsActive { get; set; }
    
    public bool IsDiscounted { get; set; }
    
    public decimal? DiscountedPrice { get; set; }   
    
    public DateTime? DiscountValidUntil { get; set; }
    
    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; internal set; }
    
    public Product Product { get; set; }
    
    public ProductListing()
    {
        
    }

    public ProductListing(decimal price)
    {
        Price = price;
        CreatedAt = DateTime.Now.ToUniversalTime();
        UpdatedAt = DateTime.Now.ToUniversalTime();
        IsActive = true;
    }

}
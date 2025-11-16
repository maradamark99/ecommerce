namespace Ecommerce.Domain.ProductManagement;

public class ProductListing
{
    
    public long Id { get; set; }
    
    public decimal PriceInEur { get; set; } 
    
    public bool IsActive { get; set; }
    
    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; internal set; }
    
    public Product Product { get; set; }
    
    public ProductListing()
    {
        
    }

    public ProductListing(decimal priceInEur)
    {
        PriceInEur = priceInEur;
        CreatedAt = DateTime.Now.ToUniversalTime();
        UpdatedAt = DateTime.Now.ToUniversalTime();
        IsActive = true;
    }

}
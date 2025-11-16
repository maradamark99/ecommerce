namespace Review.Model;

public class CustomerPurchase
{
    public string CustomerId { get; set; }
    
    public Customer Customer { get; set; }
    
    public string ProductId { get; set; }
    
    public Product Product { get; set; }    
    
    public DateTime LatestPurchaseDate { get; set; }
}
namespace Review.Model;

public class Review
{
    public long Id { get; set; }
    
    public decimal Rating { get; set; }
    
    public string Comment { get; set; }
    
    public string CustomerId { get; set; }
    
    public Customer Customer { get; set; }
    
    public Product Product { get; set; }
    
    public string ProductId { get; set; }
        
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
}
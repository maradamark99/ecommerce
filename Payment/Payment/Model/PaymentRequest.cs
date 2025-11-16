namespace Payment.Model;

public class PaymentRequest
{
    public string CustomerId { get; set; }
    
    public string OrderId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
}
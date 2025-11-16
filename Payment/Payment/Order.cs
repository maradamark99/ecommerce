using Payment.Model;

namespace Payment;

public class Order 
{
    public string OrderId { get; set; }
    
    public string CustomerId { get; set; }
    
    public string? PaymentId { get; set; }
    
    public Model.Payment? Payment { get; set; }
    public decimal TotalAmount { get; set; }
    
    public PaymentMethod SelectedPaymentMethod { get; set; }
}
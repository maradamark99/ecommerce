namespace Payment;

public class PaymentConfig
{
    public string StripeSecret { get; set; }
    
    public double CheckoutSessionTimeout { get; set; } = 30;
    
    public string SuccessUrl { get; set; } = string.Empty;
    
    public string CancelUrl { get; set; } = string.Empty;
}
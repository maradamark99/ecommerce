namespace Payment.Model;

public class PaymentInitiationResult
{
    public string? ClientSecret { get; set; }
    
    public static PaymentInitiationResult Empty()
    {
        return new PaymentInitiationResult { ClientSecret = null };
    }   
}
namespace Ecommerce.Domain.Payment;

public class PaymentInitiationResult
{
    public string? PaymentIntentId { get; set; }
    
    public string? ClientSecret { get; set; }
    
    public static PaymentInitiationResult Empty 
        => new PaymentInitiationResult { ClientSecret = null, PaymentIntentId = null };
}
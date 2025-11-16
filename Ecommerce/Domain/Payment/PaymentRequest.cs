namespace Ecommerce.Domain.Payment;

public class PaymentRequest
{
    public string OrderId { get; set; }
    public string PaymentMethod { get; set; }
}
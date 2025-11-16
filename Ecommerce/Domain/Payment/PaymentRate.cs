namespace Ecommerce.Domain.Payment;

public class PaymentRate
{
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Fee { get; set; }
}
namespace Payment.Contract;

public class PaymentRequestDto
{
    public string OrderId { get; set; }
    public string PaymentMethod { get; set; }
}
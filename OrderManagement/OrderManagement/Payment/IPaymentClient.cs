namespace OrderManagement.Payment;

public interface IPaymentClient
{
    Task<PaymentFeeResult> GetPaymentFeeAsync(string paymentMethod);
}
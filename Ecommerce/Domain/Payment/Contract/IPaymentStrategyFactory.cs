namespace Ecommerce.Domain.Payment.Contract;

public interface IPaymentStrategyFactory
{
    IPaymentStrategy CreatePaymentStrategy(string paymentMethod);
}
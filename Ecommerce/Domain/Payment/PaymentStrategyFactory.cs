using Ecommerce.Domain.Payment.Contract;

namespace Ecommerce.Domain.Payment;

public class PaymentStrategyFactory(IServiceProvider serviceProvider) : IPaymentStrategyFactory   
{
    public IPaymentStrategy CreatePaymentStrategy(string paymentMethod)
    {
        return serviceProvider.GetRequiredKeyedService<IPaymentStrategy>(paymentMethod);
    }
}
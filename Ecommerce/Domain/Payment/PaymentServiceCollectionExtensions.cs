using Ecommerce.Domain.Payment.Contract;

namespace Ecommerce.Domain.Payment;

public static class PaymentServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentServices(this IServiceCollection services)
    {
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IStripePaymentService, StripePaymentService>();
        services.AddKeyedScoped<IPaymentStrategy, AsyncPaymentStrategy>(nameof(PaymentMethod.CreditCard));
        services.AddKeyedScoped<IPaymentStrategy, PaymentOnDeliveryStrategy>(nameof(PaymentMethod.CashOnDelivery));
        services.AddScoped<IPaymentStrategyFactory, PaymentStrategyFactory>();
        return services;
    }
}
using Microsoft.Extensions.DependencyInjection;
using Payment.Contract;

namespace Payment.Tests;

public class MockKeyedServiceProvider(IPaymentStrategy strategy) : IKeyedServiceProvider
{
    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        return strategy;
    }

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
    {
        return strategy;
    }

    public object? GetService(Type serviceType)
    {
        return strategy;
    }
}
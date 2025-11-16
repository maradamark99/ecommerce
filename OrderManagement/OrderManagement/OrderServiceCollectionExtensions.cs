using OrderManagement.Contract;

namespace OrderManagement;

public static class OrderServiceCollectionExtensions
{
    public static IServiceCollection AddOrderServices(this IServiceCollection services)
    {
        services.AddScoped<IOrderManagementService, OrderManagementService>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddSingleton<IOrderDataMapper, OrderDataMapper>();
        return services;
    }
}
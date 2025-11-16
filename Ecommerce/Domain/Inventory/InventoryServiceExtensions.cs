using Ecommerce.Domain.Inventory.Contract;

namespace Ecommerce.Domain.Inventory;

public static class InventoryServiceExtensions
{
    public static IServiceCollection AddInventoryServices(this IServiceCollection services) 
    {
        services.AddScoped<IInventoryService, NoOpInventoryService>();
        return services;
    }
}
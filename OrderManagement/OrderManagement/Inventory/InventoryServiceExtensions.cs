namespace OrderManagement.Inventory;

public static class InventoryServiceExtensions
{
    public static IServiceCollection AddInventoryClient(this IServiceCollection services, IConfiguration configuration) 
    {
        services.Configure<InventoryClientConfig>(configuration.GetSection(nameof(InventoryClientConfig)));
        services.AddHttpClient<IInventoryClient, InventoryClient>();
        return services;
    }
    
}
using Ecommerce.Domain.ProductManagement.Category;
using Ecommerce.Domain.ProductManagement.Category.Contract;
using Ecommerce.Domain.ProductManagement.ProductCatalog;
using Ecommerce.Domain.ProductManagement.ProductCatalog.Contract;
using Ecommerce.Domain.ProductManagement.ProductManagement;
using Ecommerce.Domain.ProductManagement.ProductManagement.Contract;

namespace Ecommerce.Domain.ProductManagement;

public static class ProductServiceCollectionExtensions
{
    public static IServiceCollection AddProductServices(this IServiceCollection services)
    {
        services.AddSingleton<ICategoryMapper, CategoryMapper>();
        services.AddSingleton<IProductManagementMapper, ProductManagementMapper>();
        services.AddSingleton<IProductAttributesValidator, ProductAttributesValidator>();
        services.AddScoped<IProductCatalogMapper, ProductCatalogMapper>();
        services.AddScoped<IProductCatalogRepository, ProductCatalogRepository>();
        services.AddScoped<IProductCatalogService, ProductCatalogService>();
        services.AddScoped<IProductMediaRepository, ProductManagementRepository>();
        services.AddScoped<IProductListingRepository, ProductManagementRepository>();
        services.AddScoped<IProductMediaService, ProductManagementService>();
        services.AddScoped<IProductListingService, ProductManagementService>();
        services.AddScoped<IProductManagementService, ProductManagementService>();
        services.AddScoped<ICategoryService, CategoryService>(); 
        services.AddScoped<IProductManagementRepository, ProductManagementRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductManagementRepository, ProductManagementRepository>();
        services.AddScoped<IProductManagementService, ProductManagementService>();
        
        return services;
    }
}
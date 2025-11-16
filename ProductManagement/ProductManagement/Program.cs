using EcommerceLib.Contract.Events;
using EcommerceLib.Exception;
using EcommerceLib.File;
using EcommerceLib.Messaging;
using EcommerceLib.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Minio;
using ProductManagement.Category;
using ProductManagement.Category.Contract;
using ProductManagement.Contract;
using ProductManagement.Messaging;
using ProductManagement.ProductCatalog;
using ProductManagement.ProductCatalog.Contract;

namespace ProductManagement;

public class Program : ITestEnvironmentMarker
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Configuration.AddJsonFile("appsettings.Development.json");
        builder.Services.AddAuthorization();
        builder.Services.AddMemoryCache();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddControllers();
        builder.Services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });
        builder.Services.AddDbContext<AppDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseNpgsql(
                builder.Configuration.GetConnectionString("Database")
            );
        });
        
        builder.Services.Configure<FileConfig>(builder.Configuration.GetSection(nameof(FileConfig)));
        builder.Services.Configure<StorageConfig>(builder.Configuration.GetSection(nameof(StorageConfig)));
        builder.Services.Configure<ProductConfig>(builder.Configuration.GetSection(nameof(ProductConfig)));
        builder.Services.Configure<ConsumerOptions>(builder.Configuration.GetSection(nameof(ConsumerOptions)));
        builder.Services.Configure<ProducerOptions>(builder.Configuration.GetSection(nameof(ProducerOptions)));
        builder.Services.AddSingleton<IMinioClient>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<StorageConfig>>().Value;
            var minioClient = new MinioClient()
                .WithEndpoint(config.Endpoint)
                .WithCredentials(config.AccessKey, config.SecretKey)
                .Build();
            return minioClient;
        });
        builder.Services.AddExceptionHandler<ExceptionHandler>();
        builder.Services.AddSingleton<IFileInspector, NaiveFileInspector>();
        builder.Services.AddSingleton<IFileValidator, FileValidator>();
        builder.Services.AddSingleton<IStorageClient, MinioStorageClient>();
        builder.Services.AddSingleton<ICategoryMapper, CategoryMapper>();
        builder.Services.AddSingleton<IProductManagementMapper, ProductManagementMapper>();
        builder.Services.AddSingleton<IProductAttributesValidator, ProductAttributesValidator>();
        builder.Services.AddScoped<IProductCatalogMapper, ProductCatalogMapper>();
        builder.Services.AddScoped<IProductCatalogRepository, ProductCatalogRepository>();
        builder.Services.AddScoped<IProductCatalogService, ProductCatalogService>();
        builder.Services.AddScoped<IProductMediaRepository, ProductManagementRepository>();
        builder.Services.AddScoped<IProductListingRepository, ProductManagementRepository>();
        builder.Services.AddScoped<IProductMediaService, ProductManagementService>();
        builder.Services.AddScoped<IProductListingService, ProductManagementService>();
        builder.Services.AddScoped<IProductManagementService, ProductManagementService>();
        builder.Services.AddScoped<ICategoryService, CategoryService>(); 
        builder.Services.AddScoped<IProductManagementRepository, ProductManagementRepository>();
        builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
        builder.Services.AddScoped<IProductManagementRepository, ProductManagementRepository>();
        builder.Services.AddScoped<IProductManagementService, ProductManagementService>();
        builder.Services.AddSingleton<IEventProducer<ProductEventDto>, ProductManagementEventProducer>();
        builder.Services.AddHostedService<InventoryEventConsumerService>();

        var app = builder.Build();

        app.MapControllers();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.UseExceptionHandler(_ => { });


        app.Run();
    }
}
using System.Text.Json;
using Cart.Contract;
using Microsoft.Extensions.Options;
using Minio.Exceptions;

namespace Cart;

public class ProductManagementClient : IProductManagementClient
{
    private readonly ILogger<ProductManagementClient> _logger;
    private readonly HttpClient _httpClient;

    public ProductManagementClient(
        HttpClient httpClient, 
        IOptions<ProductManagementClientConfig> clientConfig, 
        ILogger<ProductManagementClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(clientConfig.Value.BaseUrl);
        _logger = logger;
    }

    public async Task<List<ProductDto>> GetProductsByIdsAsync(IEnumerable<string> productIds)
    {
        var url = $"api/v1/products/batch/{string.Join(",", productIds)}";
        _logger.LogInformation("Calling {url}", url);

        var response = await _httpClient.GetAsync(url);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            _logger.LogInformation("No content returned for productIds");
            return null;
        }

        response.EnsureSuccessStatusCode();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>(options);
        
        if (products is null)
        {
            throw new InternalServerException("No product details returned from API.");
        }
        _logger.LogInformation("Retrieved {Count} products: {Products}", products.Count, JsonSerializer.Serialize(products, options));

        return products;
    }
}
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Wishlist.Contract;

namespace Wishlist;

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

    public async Task<ProductDto?> GetProductByIdAsync(string productId)
    {
        var url = $"api/v1/products/{productId}";
        _logger.LogInformation("Calling {url}", url);

        var response = await _httpClient.GetAsync(url);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            _logger.LogInformation("No content returned for productId {productId}", productId);
            return null;
        }

        response.EnsureSuccessStatusCode();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var product = await response.Content.ReadFromJsonAsync<ProductDto>(options);
        if (product is null)
        {
            throw new InvalidOperationException("No product details returned from API.");
        }

        return product;
    }
}
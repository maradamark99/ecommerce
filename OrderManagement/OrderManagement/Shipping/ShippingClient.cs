using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using OrderManagement.Shipping.Contract;

namespace OrderManagement.Shipping;

public class ShippingClient : IShippingClient
{
    
    private readonly ILogger<ShippingClient> _logger;
    
    private readonly HttpClient _httpClient;
    
    private readonly IOptions<ShippingClientConfig> _clientConfig;

    public ShippingClient(HttpClient httpClient, IOptions<ShippingClientConfig> clientConfig, ILogger<ShippingClient> logger)
    {
        _httpClient = httpClient;
        _clientConfig = clientConfig;
        _httpClient.BaseAddress = new Uri(clientConfig.Value.BaseUrl);
        _logger = logger;
    }

    public async Task<ShippingFeeResult> GetShippingFeeAsync(string shippingMethod)
    {
        var url = $"{_clientConfig.Value.Endpoint}/fee/{shippingMethod}";
        _logger.LogInformation("Calling {url}", url);
        var response = await _httpClient.GetAsync(url);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));

        ShippingFeeDto? rate = null;
        if (response.Content.Headers.ContentLength > 0)
        {
            rate = await response.Content.ReadFromJsonAsync<ShippingFeeDto>(options);
        }
        _logger.LogInformation("Shipping API returned: {rate}", JsonSerializer.Serialize(rate, options));

        return new ShippingFeeResult
        {
            StatusCode = response.StatusCode,
            Fee = rate
        };
    }
}
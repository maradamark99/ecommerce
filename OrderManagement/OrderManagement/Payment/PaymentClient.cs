using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using OrderManagement.Contract;

namespace OrderManagement.Payment;

public class PaymentClient : IPaymentClient
{
    
    private readonly ILogger<PaymentClient> _logger;
    
    private readonly HttpClient _httpClient;
    
    private readonly IOptions<PaymentClientConfig> _clientConfig;
    public PaymentClient(HttpClient httpClient, IOptions<PaymentClientConfig> clientConfig, ILogger<PaymentClient> logger)
    {
        _httpClient = httpClient;
        _clientConfig = clientConfig;
        _httpClient.BaseAddress = new Uri(clientConfig.Value.BaseUrl);
        _logger = logger;
    }
    
    public async Task<PaymentFeeResult> GetPaymentFeeAsync(string paymentMethod)
    {
        var url = $"{_clientConfig.Value.Endpoint}/fee/{paymentMethod}";

        _logger.LogInformation("Calling Payment API for method: {method} at {url}", paymentMethod, url);

        var response = await _httpClient.GetAsync(url);

        PaymentFeeDto? rate = null;
        if (response.Content.Headers.ContentLength > 0)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));

            rate = await response.Content.ReadFromJsonAsync<PaymentFeeDto>(options);
        }
        _logger.LogInformation("Payment API returned: {rate}", JsonSerializer.Serialize(rate));

        return new PaymentFeeResult
        {
            StatusCode = response.StatusCode,
            Fee = rate
        };
    }
}
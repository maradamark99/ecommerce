using System.Text.Json;
using EcommerceLib.Exception;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Notification.Clients;

public class ProfileClient : IProfileClient
{
    private readonly ILogger<ProfileClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _memoryCache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);

    public ProfileClient(
        HttpClient httpClient, 
        IOptions<ProfileClientConfig> clientConfig,
        IMemoryCache memoryCache,
        ILogger<ProfileClient> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(clientConfig.Value.BaseUrl);
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<CustomerDetailsDto?> GetCustomerDetailsAsync(string customerId)
    {
        if (_memoryCache.TryGetValue(customerId, out CustomerDetailsDto? cachedCustomer))
        {
            _logger.LogInformation("Returning cached data for customerId {customerId}", customerId);
            return cachedCustomer;
        }

        var url = $"api/v1/profile/{customerId}";
        
        _logger.LogInformation("Calling {url}", url);

        var response = await _httpClient.GetAsync(url);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            _logger.LogInformation("No content returned for customerId {customerId}", customerId);
            return null;
        }

        response.EnsureSuccessStatusCode();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var customerDetails = await response.Content.ReadFromJsonAsync<CustomerDetailsDto>(options);
        if (customerDetails is null)
        {
            throw new  NotFoundException("No customer details returned from API.");
        }
        _memoryCache.Set(customerId, customerDetails, _cacheDuration);

        return customerDetails;
    }
}
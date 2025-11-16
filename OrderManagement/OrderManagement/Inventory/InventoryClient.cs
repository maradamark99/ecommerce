using Microsoft.Extensions.Options;

namespace OrderManagement.Inventory;

public class InventoryClient : IInventoryClient
{

    private readonly HttpClient _client;
    private readonly ILogger<InventoryClient> _logger;
    private readonly IOptions<InventoryClientConfig> _config;


    public InventoryClient(HttpClient client, ILogger<InventoryClient> logger, IOptions<InventoryClientConfig> config)
    {
        _client = client;
        client.BaseAddress = new Uri(config.Value.BaseUrl);
        _logger = logger;
        _config = config;
    }

    public async Task<HttpResponseMessage> TryReserveStockAsync(StockReservationRequest reservationRequest)
    {
        var url = $"{_config.Value.Endpoint}/reserve-stock";
        _logger.LogInformation("Calling {url}", url);   
        var response = await _client.PostAsJsonAsync(url, reservationRequest);
        _logger.LogInformation("Stock reserved successfully for request: {request}", reservationRequest);
        return response;
    }
}
using EcommerceLib.Contract;
using EcommerceLib.Contract.Dto;
using EcommerceLib.Contract.Events;
using EcommerceLib.Exception;
using EcommerceLib.Messaging;
using Shipping.Contract;

namespace Shipping;

public class MockShippingService(
    ILogger<MockShippingService> logger,    
    EventProducerBase<ShippingEventDto> eventProducer) : IShippingService
{
    public Task<ShippingRateDto> GetShippingFeeAsync(ShippingMethod shippingMethod)
    {
        var shippingRate = shippingMethod switch
        {
            ShippingMethod.Standard => new ShippingRateDto()
            {
                Fee = 5,
                Method = shippingMethod.ToString(),
                EstimatedShippingDate = DateTime.UtcNow.AddDays(5)
            },
            ShippingMethod.Express => new ShippingRateDto()
            {
                Fee = 10,
                Method = shippingMethod.ToString(),
                EstimatedShippingDate = DateTime.UtcNow.AddDays(2)
            },
            ShippingMethod.Pickup => new ShippingRateDto()
            {
                Fee = 0,
                Method = shippingMethod.ToString(),
            },
            _ => throw new NotFoundException($"{shippingMethod} is not found")
        };
        return Task.FromResult(shippingRate);
    }

    public async Task CreateShipmentAsync(CreateShipmentRequest shipmentRequest)
    {
        logger.LogInformation("Creating shipment for order: {orderId}, correlationId: {correlationId}", shipmentRequest.OrderId, shipmentRequest.CorrelationId); 
        await SimulateShipmentAsync(shipmentRequest);
    }

    public Task CancelShipmentAsync(string orderId)
    {
        return Task.CompletedTask;
    }

    private async Task SimulateShipmentAsync(CreateShipmentRequest shipmentRequest)
    {
        var customerDto = new CustomerDto()
        {
            CustomerId = shipmentRequest.CustomerDetails.CustomerId,
        };
        var itemsDto = shipmentRequest.Items.Select(i => new ItemDto()
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity
        }).ToList();
        var shipmentId = Guid.NewGuid().ToString();
        await Task.Delay(TimeSpan.FromSeconds(2));
        await eventProducer.ProduceAsync(new ShippingEventDto
        {
            ShipmentId = shipmentId,
            EventId = Guid.NewGuid().ToString(),
            EventType = nameof(Events.ShipmentCreated),
            ShipmentStatus = "Created",
            OrderId = shipmentRequest.OrderId,
            Customer = customerDto,
            Items = itemsDto
        });
        logger.LogInformation("Delivering shipment for order: {orderId}, correlationId: {correlationId}", shipmentRequest.OrderId, shipmentRequest.CorrelationId);

        await Task.Delay(TimeSpan.FromSeconds(3));
        await eventProducer.ProduceAsync(new ShippingEventDto
        {
            ShipmentId = shipmentId,
            EventId = Guid.NewGuid().ToString(),
            ShipmentStatus = "Delivered",
            EventType = nameof(Events.ShipmentDelivered),
            OrderId = shipmentRequest.OrderId,
            Customer = customerDto,
            Items = itemsDto
        });
        logger.LogInformation("Shipment has been delivered for order: {orderId}, correlationId: {correlationId}", shipmentRequest.OrderId, shipmentRequest.CorrelationId);
    }
}
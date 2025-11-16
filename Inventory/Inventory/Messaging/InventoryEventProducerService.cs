using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Options;

namespace Inventory.Messaging;

public class InventoryEventProducerService(
    IOptions<ProducerOptions> options,
    ILogger<EventProducerBase<InventoryEventDto>> logger)
    : EventProducerBase<InventoryEventDto>(options, logger);
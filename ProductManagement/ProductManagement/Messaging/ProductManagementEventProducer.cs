using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Options;

namespace ProductManagement.Messaging;

public class ProductManagementEventProducer(
    IOptions<ProducerOptions> options,
    ILogger<EventProducerBase<ProductEventDto>> logger)
    : EventProducerBase<ProductEventDto>(options, logger);
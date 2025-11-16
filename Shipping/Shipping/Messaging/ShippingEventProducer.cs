using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Options;

namespace Shipping.Messaging;

public class ShippingEventProducer(
    IOptions<ProducerOptions> options,
    ILogger<EventProducerBase<ShippingEventDto>> logger)
    : EventProducerBase<ShippingEventDto>(options, logger);
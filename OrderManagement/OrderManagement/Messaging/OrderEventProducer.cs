using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Options;

namespace OrderManagement.Messaging;

public class OrderEventProducer(
    IOptions<ProducerOptions> options,
    ILogger<EventProducerBase<OrderEventDto>> logger)
    : EventProducerBase<OrderEventDto>(options, logger);
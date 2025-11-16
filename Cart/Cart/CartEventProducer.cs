using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Options;

namespace Cart;

public class CartEventProducer(
    IOptions<ProducerOptions> options,
    ILogger<EventProducerBase<CartCheckedOutEventDto>> logger)
    : EventProducerBase<CartCheckedOutEventDto>(options, logger);
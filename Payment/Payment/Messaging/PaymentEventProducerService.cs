using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Options;

namespace Payment.Messaging;

public class PaymentEventProducerService(
    IOptions<ProducerOptions> options,
    ILogger<EventProducerBase<PaymentEventDto>> logger)
    : EventProducerBase<PaymentEventDto>(options, logger);
using EcommerceLib.Contract.Events;
using EcommerceLib.Messaging;
using Microsoft.Extensions.Options;

namespace Auth;

public class AuthEventProducer(IOptions<ProducerOptions> options, ILogger<EventProducerBase<AuthEventDto>> logger)
    : EventProducerBase<AuthEventDto>(options, logger);
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EcommerceLib.Messaging;

public abstract class EventProducerBase<TMessage> : IEventProducer<TMessage> where TMessage : IEventMessage    
{
    
    private readonly IProducer<string, string> _producer;
    
    private readonly ILogger<EventProducerBase<TMessage>> _logger;

    private readonly IOptions<ProducerOptions> _options;

    protected EventProducerBase(IOptions<ProducerOptions> options, ILogger<EventProducerBase<TMessage>> logger)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Enum.Parse<Acks>(options.Value.Acks, ignoreCase: true),
            MessageSendMaxRetries = options.Value.MessageSendMaxRetries,
            EnableIdempotence = options.Value.EnableIdempotence,
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
        _options = options;
        _logger = logger;   
    }
    
    public async Task ProduceAsync(TMessage message, CancellationToken cancellationToken = default)
    {

        var value = JsonSerializer.Serialize(message);
        try
        {
            var result = await _producer.ProduceAsync(_options.Value.Topic, new Message<string, string>
            {
                Key = message.EventId,
                Value = value
            }, cancellationToken);
            _logger.LogInformation("Produced message to {Topic} at offset {Offset}", result.Topic, result.Offset);
        }
        catch (System.Exception ex)
        {
            _logger.LogError("Error producing message: {Message}", ex.Message);
        }
    }
}
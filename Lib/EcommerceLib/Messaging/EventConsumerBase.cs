using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EcommerceLib.Messaging;

public abstract class EventConsumerBase<TMessage> : BackgroundService where TMessage : IEventMessage
{
    private readonly IOptions<ConsumerOptions> _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger _logger;
    private readonly IConsumer<string, string> _consumer;

    protected EventConsumerBase(
        IOptions<ConsumerOptions> options,
        IMemoryCache cache,
        ILogger logger)
    {
        _options = options;
        _cache = cache;
        _logger = logger;
        var config = CreateConfig();
        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) 
        => Task.Run(() => ConsumeAsync(stoppingToken), stoppingToken);

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting consumer service");
        
        _consumer.Subscribe(_options.Value.Topic);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var cr = _consumer.Consume(cancellationToken);
                TMessage? msg;
                try
                {
                    msg = JsonSerializer.Deserialize<TMessage>(
                        cr.Message.Value,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (System.Exception e)
                {
                    _logger.LogError(e.Message);
                    continue;
                }
                if (msg == null)
                {
                    _logger.LogWarning("Received invalid message: {Value}", cr.Message.Value);
                    continue;
                }

                var msgId = msg.EventId;
                if (_cache.TryGetValue(msgId, out _))
                {
                    _logger.LogInformation("Duplicate message {Id} skipped", msgId);
                }
                else
                {
                    _logger.LogInformation("Consuming message {MsgId}", msgId);
                    await HandleMessageAsync(msg, cancellationToken);
                    _cache.Set(msgId, true, TimeSpan.FromMinutes(10));
                    _consumer.Commit(cr);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _logger.LogInformation("Stopping consumer service");
            _consumer.Close();
        }
    }

    private ConsumerConfig CreateConfig() =>
        new()
        {
            BootstrapServers = _options.Value.BootstrapServers,
            GroupId = _options.Value.GroupId,
            AutoOffsetReset = Enum.TryParse<AutoOffsetReset>(_options.Value.AutoOffsetReset, true, out var aor)
                ? aor
                : AutoOffsetReset.Earliest,
            EnableAutoCommit = _options.Value.EnableAutoCommit,
            AllowAutoCreateTopics = _options.Value.AllowAutoCreateTopics,
        };

    protected abstract Task HandleMessageAsync(TMessage msg, CancellationToken cancellationToken);

}
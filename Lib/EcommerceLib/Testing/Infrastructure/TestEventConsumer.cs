using System.Collections.Concurrent;
using System.Text.Json;
using Confluent.Kafka;

namespace EcommerceLib.Testing.Infrastructure;

public class TestEventConsumer : IDisposable
{
    private readonly IConsumer<string, string> _consumer;
    private ConcurrentBag<object> _messages = [];

    public TestEventConsumer(string bootstrapServers, string? groupId = null)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId ?? "test-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    public void ConsumeWithTimeout<T>(string topic, TimeSpan consumeDuration)
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(consumeDuration);

        _consumer.Subscribe(topic);
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var cr = _consumer.Consume(cts.Token);
                if (cr?.Message != null)
                {
                    try
                    {
                        var obj = JsonSerializer.Deserialize<T>(cr.Message.Value);
                        if (obj != null)
                        {
                            _messages.Add(obj);
                        }
                    }
                    catch (JsonException ex)
                    {
                        break;                       
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public bool HasMessageContaining<T>(Predicate<T> match)
    {
        foreach (var message in _messages)
        {
            if (message is T typedMessage && match(typedMessage))
                return true;
        }
        return false;
    }

    public void Reset()
    {
        _messages = [];
    }

    public void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
    }
}
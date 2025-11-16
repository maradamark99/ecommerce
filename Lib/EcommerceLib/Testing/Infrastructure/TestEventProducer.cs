using Confluent.Kafka;

namespace EcommerceLib.Testing.Infrastructure;

public class TestEventProducer(string bootstrapServers) : IAsyncDisposable
{
    private IProducer<string, string>? _producer;

    public Task StartAsync()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            AllowAutoCreateTopics = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        return Task.CompletedTask;
    }

    public Task ProduceAsync(string topic, object msg, string? key = null)
    {
        if (_producer is null)
            throw new InvalidOperationException("Producer not started. Call StartAsync() first.");

        var message = new Message<string, string>
        {
            Key = key ?? "",
            Value = System.Text.Json.JsonSerializer.Serialize(msg)
        };

        return _producer.ProduceAsync(topic, message);
    }

    public async ValueTask DisposeAsync()
    {
        if (_producer is not null)
        {
            _producer.Flush(TimeSpan.FromSeconds(10));
            _producer.Dispose();
        }

        await Task.CompletedTask;
    }
}
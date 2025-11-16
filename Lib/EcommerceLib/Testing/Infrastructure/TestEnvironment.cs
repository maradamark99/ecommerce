using Confluent.Kafka;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Server;
using DotNet.Testcontainers.Builders;
using EcommerceLib.Testing.Auth;
using Microsoft.Extensions.Configuration;
using Testcontainers.Kafka;

namespace EcommerceLib.Testing.Infrastructure;

public class TestEnvironment<T> : WebApplicationFactory<T> where T : class
{
    internal bool UseProducer { get; set; }
    internal bool UseConsumer { get; set; }
    internal bool UseDatabase { get; set; }
    internal bool UseMockServer { get; set; }

    internal Action<IServiceCollection>? ConfigureServices { get; set; }
    private TestEnvironmentBuilder<T> CreateBuilder => new(this);

    public WireMockServer? MockServer { get; internal set; }
    public TestEventProducer? EventProducer { get; internal set; }
    public TestEventConsumer? EventConsumer { get; internal set; }
    public TestDatabase? TestDatabase { get; internal set; }
    private KafkaContainer? KafkaContainer { get; set; }

    public static TestEnvironmentBuilder<T> Builder => new TestEnvironment<T>().CreateBuilder;

    private TestEnvironment() { }

    public AuthorizedClientBuilder CreateAuthorizedClientBuilder() =>
        new(CreateClient());

    public async Task StartAsync()
    {
        if (UseDatabase)
        {
            await TestDatabase!.StartAsync();
        }

        if (UseProducer || UseConsumer)
        {
            KafkaContainer = new KafkaBuilder()
                .WithPortBinding(9092)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(9092))
                .Build();

            await KafkaContainer.StartAsync();

            if (UseProducer)
            {
                EventProducer = new TestEventProducer(KafkaContainer.GetBootstrapAddress());
                await EventProducer.StartAsync();

                await CreateTopicsAsync(Topics.All());
            }

            if (UseConsumer)
            {
                EventConsumer = new TestEventConsumer(KafkaContainer.GetBootstrapAddress());
            }
        }

        if (UseMockServer)
        {
            MockServer = WireMockServer.Start();
        }
        _ = Services; // Trigger service provider creation
        if (UseDatabase)
        {
            await TestDatabase!.CreateCheckpointAsync();
        }
    }

    public async Task ResetAsync()
    {
        if (UseMockServer)
        {
            MockServer?.ResetMappings();
        }

        if (UseDatabase)
        {
            await TestDatabase!.ResetAsync();
        }

        EventConsumer?.Reset();
    }

    public override async ValueTask DisposeAsync()
    {
        if (UseProducer && EventProducer is not null)
        {
            await EventProducer.DisposeAsync();
        }

        if (UseConsumer && EventConsumer is not null)
        {
            EventConsumer.Dispose();
        }

        if ((UseProducer || UseConsumer) && KafkaContainer is not null)
        {
            await KafkaContainer.StopAsync();
            await KafkaContainer.DisposeAsync();
        }

        if (UseDatabase && TestDatabase is not null)
        {
            await TestDatabase.StopAsync();
        }

        if (UseMockServer && MockServer is not null)
        {
            MockServer.Stop();
            MockServer.Dispose();
        }

        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.Sources.Clear();
            config.AddJsonFile("appsettings.Testing.json", optional: false, reloadOnChange: false);
        });
        builder.ConfigureServices(services =>
        {
            ConfigureServices?.Invoke(services);
        });
    }

    private async Task CreateTopicsAsync(IEnumerable<string> topics, int partitions = 1, short replicationFactor = 1)
    {
        var config = new AdminClientConfig
        {
            BootstrapServers = KafkaContainer!.GetBootstrapAddress()
        };

        using var adminClient = new AdminClientBuilder(config).Build();

        var specs = topics.Select(t => new Confluent.Kafka.Admin.TopicSpecification
        {
            Name = t,
            NumPartitions = partitions,
            ReplicationFactor = replicationFactor
        });

        try
        {
            await adminClient.CreateTopicsAsync(specs);
        }
        catch (Confluent.Kafka.Admin.CreateTopicsException e)
        {
            foreach (var result in e.Results)
            {
                if (result.Error.Code != ErrorCode.TopicAlreadyExists)
                {
                    throw;
                }
            }
        }
    }
}

public class TestEnvironmentBuilder<T>(TestEnvironment<T> environment) where T : class
{
    public TestEnvironmentBuilder<T> WithProducer()
    {
        environment.UseProducer = true;
        return this;
    }

    public TestEnvironmentBuilder<T> WithConsumer()
    {
        environment.UseConsumer = true;
        return this;
    }

    public TestEnvironmentBuilder<T> WithDatabase<TDb>() where TDb : DbContext
    {
        environment.UseDatabase = true;
        environment.TestDatabase = new TestDatabase();

        environment.ConfigureServices += services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TDb>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<TDb>(opts =>
                environment.TestDatabase!.ConfigureDbContext(opts));

            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TDb>();
            db.Database.Migrate();
        };

        return this;
    }

    public TestEnvironmentBuilder<T> WithMockServer()
    {
        environment.UseMockServer = true;
        return this;
    }

    public TestEnvironmentBuilder<T> WithServiceOverride(Action<IServiceCollection> configure)
    {
        environment.ConfigureServices += configure;
        return this;
    }

    public TestEnvironment<T> Build() => environment;
}
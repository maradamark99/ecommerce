using System.Data.Common;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace EcommerceLib.Testing.Infrastructure;

public class TestDatabase 
{
    private PostgreSqlContainer? _postgres;
    private DbConnection? _connection;
    private Respawner? _respawner;

    public async Task StartAsync()
    {
        _postgres = new PostgreSqlBuilder()
            .WithDatabase("Database")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithPortBinding(5432)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5432))
            .Build();

        await _postgres.StartAsync();
        _connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await _connection.OpenAsync();
    }

    public void ConfigureDbContext(DbContextOptionsBuilder builder)
    {
        if (_postgres != null)
        {
            builder.UseNpgsql(_postgres.GetConnectionString()); 
        }
    }
    
    public async Task CreateCheckpointAsync()
    {
        if (_connection != null)
        {
            _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
            });
        }
    }

    public async Task ResetAsync()
    {
        if (_respawner != null && _connection != null)
        {
            await _respawner.ResetAsync(_connection);
        }
    }

    public async Task StopAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }

        if (_postgres != null)
        {
            await _postgres.StopAsync();    
            await _postgres.DisposeAsync();
        }
    }
}
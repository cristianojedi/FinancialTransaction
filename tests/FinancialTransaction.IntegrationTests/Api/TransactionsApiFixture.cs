using FinancialTransaction.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;

namespace FinancialTransaction.IntegrationTests.Api;

public class TransactionsApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("financialtransaction")
        .WithUsername("financialtransaction")
        .WithPassword("financialtransaction")
        .Build();

    private readonly KafkaContainer _kafkaContainer = new KafkaBuilder("confluentinc/cp-kafka:7.7.1")
        .Build();

    private WebApplicationFactory<Program>? _factory;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_container.StartAsync(), _kafkaContainer.StartAsync());

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:PostgreSql", _container.GetConnectionString());
            builder.UseSetting("Kafka:BootstrapServers", _kafkaContainer.GetBootstrapAddress());
            builder.UseSetting("Kafka:TransactionsTopic", "financial.transactions.created");
        });

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _container.DisposeAsync();
        await _kafkaContainer.DisposeAsync();
    }

    public HttpClient CreateClient() => _factory!.CreateClient();

    public FinancialTransactionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinancialTransactionDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new FinancialTransactionDbContext(options);
    }
}

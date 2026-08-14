using FinancialTransaction.Application;
using FinancialTransaction.Infrastructure;
using FinancialTransaction.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;

namespace FinancialTransaction.IntegrationTests.EndToEnd;

/// <summary>
/// Sobe a infraestrutura real (PostgreSQL + Kafka) e as duas aplicações que compõem
/// o fluxo financeiro completo: a API (Blazor -> API -> PostgreSQL -> Kafka) e o
/// Worker (Kafka -> Worker -> PostgreSQL), permitindo testar o fluxo ponta a ponta.
/// </summary>
public sealed class FullFlowFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("financialtransaction")
        .WithUsername("financialtransaction")
        .WithPassword("financialtransaction")
        .Build();

    private readonly KafkaContainer _kafka = new KafkaBuilder("confluentinc/cp-kafka:7.7.1")
        .Build();

    private WebApplicationFactory<Program>? _apiFactory;
    private IHost? _workerHost;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _kafka.StartAsync());

        _apiFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:PostgreSql", _postgres.GetConnectionString());
            builder.UseSetting("Kafka:BootstrapServers", _kafka.GetBootstrapAddress());
            builder.UseSetting("Kafka:TransactionsTopic", "financial.transactions.created");
        });

        await using (var dbContext = CreateDbContext())
        {
            await dbContext.Database.MigrateAsync();
        }

        var workerBuilder = Host.CreateApplicationBuilder();
        workerBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgreSql"] = _postgres.GetConnectionString(),
            ["Kafka:BootstrapServers"] = _kafka.GetBootstrapAddress(),
            ["Kafka:TransactionsTopic"] = "financial.transactions.created",
            ["Kafka:ConsumerGroupId"] = "financial-transaction-worker-e2e",
        });

        workerBuilder.Services.AddApplication();
        workerBuilder.Services.AddInfrastructure(workerBuilder.Configuration);
        workerBuilder.Services.AddHostedService<FinancialTransaction.Worker.Worker>();

        _workerHost = workerBuilder.Build();
        await _workerHost.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_workerHost is not null)
        {
            await _workerHost.StopAsync();
            _workerHost.Dispose();
        }

        if (_apiFactory is not null)
        {
            await _apiFactory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
        await _kafka.DisposeAsync();
    }

    public HttpClient CreateApiClient() => _apiFactory!.CreateClient();

    public FinancialTransactionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinancialTransactionDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new FinancialTransactionDbContext(options);
    }
}

using Testcontainers.PostgreSql;

namespace FinancialTransaction.IntegrationTests.Persistence;

public class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("financialtransaction")
        .WithUsername("financialtransaction")
        .WithPassword("financialtransaction")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

using FinancialTransaction.Domain.Enums;
using FinancialTransaction.Infrastructure.Persistence;
using FinancialTransaction.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinancialTransaction.IntegrationTests.Persistence;

public class FinancialTransactionRepositoryTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public FinancialTransactionRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddAsync_persiste_transacao_e_permite_recuperar_por_id()
    {
        var transaction = Domain.Entities.FinancialTransaction.Create(
            sourceAccountId: Guid.NewGuid(),
            destinationAccountId: Guid.NewGuid(),
            amount: 150.75m);

        await using (var writeContext = CreateDbContext())
        {
            var repository = new FinancialTransactionRepository(writeContext);
            var unitOfWork = new UnitOfWork(writeContext);

            await repository.AddAsync(transaction);
            await unitOfWork.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext();
        var readRepository = new FinancialTransactionRepository(readContext);
        var persisted = await readRepository.GetByIdAsync(transaction.Id);

        Assert.NotNull(persisted);
        Assert.Equal(transaction.SourceAccountId, persisted!.SourceAccountId);
        Assert.Equal(transaction.DestinationAccountId, persisted.DestinationAccountId);
        Assert.Equal(transaction.Amount, persisted.Amount);
        Assert.Equal(TransactionStatus.Pending, persisted.Status);
    }

    [Fact]
    public async Task GetAllAsync_retorna_todas_as_transacoes_persistidas()
    {
        var first = Domain.Entities.FinancialTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), 10m);
        var second = Domain.Entities.FinancialTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), 20m);

        await using (var writeContext = CreateDbContext())
        {
            var repository = new FinancialTransactionRepository(writeContext);
            var unitOfWork = new UnitOfWork(writeContext);

            await repository.AddAsync(first);
            await repository.AddAsync(second);
            await unitOfWork.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext();
        var readRepository = new FinancialTransactionRepository(readContext);
        var all = await readRepository.GetAllAsync();

        Assert.Contains(all, t => t.Id == first.Id);
        Assert.Contains(all, t => t.Id == second.Id);
    }

    private FinancialTransactionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinancialTransactionDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        return new FinancialTransactionDbContext(options);
    }
}

using FinancialTransaction.Domain.Entities;
using FinancialTransaction.Infrastructure.Persistence;
using FinancialTransaction.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinancialTransaction.IntegrationTests.Persistence;

public class AccountRepositoryTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public AccountRepositoryTests(PostgreSqlFixture fixture)
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
    public async Task AddAsync_persiste_conta_e_permite_recuperar_por_id()
    {
        var account = Account.Create($"ACC-{Guid.NewGuid():N}");

        await using (var writeContext = CreateDbContext())
        {
            var repository = new AccountRepository(writeContext);
            var unitOfWork = new UnitOfWork(writeContext);

            await repository.AddAsync(account);
            await unitOfWork.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext();
        var readRepository = new AccountRepository(readContext);
        var persisted = await readRepository.GetByIdAsync(account.Id);

        Assert.NotNull(persisted);
        Assert.Equal(account.Number, persisted!.Number);
    }

    private FinancialTransactionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinancialTransactionDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        return new FinancialTransactionDbContext(options);
    }
}

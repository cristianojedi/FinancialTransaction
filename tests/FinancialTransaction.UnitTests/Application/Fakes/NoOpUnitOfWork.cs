using FinancialTransaction.Application.Abstractions.Persistence;

namespace FinancialTransaction.UnitTests.Application.Fakes;

public class NoOpUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
}

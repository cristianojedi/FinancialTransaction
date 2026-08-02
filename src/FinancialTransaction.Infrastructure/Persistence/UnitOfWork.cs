using FinancialTransaction.Application.Abstractions.Persistence;

namespace FinancialTransaction.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly FinancialTransactionDbContext _dbContext;

    public UnitOfWork(FinancialTransactionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}

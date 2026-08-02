using FinancialTransaction.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinancialTransaction.Infrastructure.Persistence.Repositories;

public class FinancialTransactionRepository : IFinancialTransactionRepository
{
    private readonly FinancialTransactionDbContext _dbContext;

    public FinancialTransactionRepository(FinancialTransactionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Domain.Entities.FinancialTransaction transaction, CancellationToken cancellationToken = default) =>
        await _dbContext.Transactions.AddAsync(transaction, cancellationToken);

    public async Task<Domain.Entities.FinancialTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _dbContext.Transactions.FirstOrDefaultAsync(transaction => transaction.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Domain.Entities.FinancialTransaction>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Transactions.AsNoTracking().ToListAsync(cancellationToken);

    public Task DeleteAsync(Domain.Entities.FinancialTransaction transaction, CancellationToken cancellationToken = default)
    {
        _dbContext.Transactions.Remove(transaction);

        return Task.CompletedTask;
    }
}

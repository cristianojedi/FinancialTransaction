using FinancialTransaction.Application.Abstractions.Persistence;
using FinancialTransaction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancialTransaction.Infrastructure.Persistence.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly FinancialTransactionDbContext _dbContext;

    public AccountRepository(FinancialTransactionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default) =>
        await _dbContext.Accounts.AddAsync(account, cancellationToken);

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _dbContext.Accounts.FirstOrDefaultAsync(account => account.Id == id, cancellationToken);
}

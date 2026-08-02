using FinancialTransaction.Domain.Entities;

namespace FinancialTransaction.Application.Abstractions.Persistence;

public interface IAccountRepository
{
    Task AddAsync(Account account, CancellationToken cancellationToken = default);

    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

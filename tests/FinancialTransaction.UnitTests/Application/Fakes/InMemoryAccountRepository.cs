using FinancialTransaction.Application.Abstractions.Persistence;
using FinancialTransaction.Domain.Entities;

namespace FinancialTransaction.UnitTests.Application.Fakes;

public class InMemoryAccountRepository : IAccountRepository
{
    private readonly Dictionary<Guid, Account> _accounts = [];

    public Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        _accounts[account.Id] = account;
        return Task.CompletedTask;
    }

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_accounts.GetValueOrDefault(id));

    public Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Account>>(_accounts.Values.ToList());
}

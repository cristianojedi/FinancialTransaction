using FinancialTransaction.Application.Abstractions.Persistence;
using FinancialTransaction.Application.Accounts.Dtos;

namespace FinancialTransaction.Application.Accounts;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;

    public AccountService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<IReadOnlyList<AccountResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _accountRepository.GetAllAsync(cancellationToken);

        return accounts.Select(AccountResponse.FromDomain).ToList();
    }
}

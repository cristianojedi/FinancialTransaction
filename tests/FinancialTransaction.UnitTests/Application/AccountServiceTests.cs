using FinancialTransaction.Application.Accounts;
using FinancialTransaction.Domain.Entities;
using FinancialTransaction.UnitTests.Application.Fakes;

namespace FinancialTransaction.UnitTests.Application;

public class AccountServiceTests
{
    private readonly InMemoryAccountRepository _accountRepository = new();
    private readonly IAccountService _sut;

    public AccountServiceTests()
    {
        _sut = new AccountService(_accountRepository);
    }

    [Fact]
    public async Task GetAllAsync_sem_contas_cadastradas_retorna_lista_vazia()
    {
        var result = await _sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_retorna_todas_as_contas_cadastradas()
    {
        var first = Account.Create("ACC-001");
        var second = Account.Create("ACC-002");
        await _accountRepository.AddAsync(first);
        await _accountRepository.AddAsync(second);

        var result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, account => account.Id == first.Id && account.Number == first.Number);
        Assert.Contains(result, account => account.Id == second.Id && account.Number == second.Number);
    }
}

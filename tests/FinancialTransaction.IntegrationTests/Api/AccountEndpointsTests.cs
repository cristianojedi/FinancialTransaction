using System.Net;
using System.Net.Http.Json;
using FinancialTransaction.Application.Accounts.Dtos;
using FinancialTransaction.Domain.Entities;

namespace FinancialTransaction.IntegrationTests.Api;

public class AccountEndpointsTests : IClassFixture<TransactionsApiFixture>
{
    private readonly TransactionsApiFixture _fixture;

    public AccountEndpointsTests(TransactionsApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GET_lista_retorna_200_com_contas_cadastradas()
    {
        var account = await SeedAccountAsync();
        var client = _fixture.CreateClient();

        var response = await client.GetAsync("/api/accounts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<AccountResponse>>();
        Assert.NotNull(body);
        Assert.Contains(body!, a => a.Id == account.Id && a.Number == account.Number);
    }

    private async Task<Account> SeedAccountAsync()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var account = Account.Create($"ACC-{Guid.NewGuid():N}");

        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();

        return account;
    }
}

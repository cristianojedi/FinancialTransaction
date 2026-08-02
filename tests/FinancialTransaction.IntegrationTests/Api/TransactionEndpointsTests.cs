using System.Net;
using System.Net.Http.Json;
using FinancialTransaction.Application.Transactions.Dtos;
using FinancialTransaction.Domain.Entities;
using FinancialTransaction.Domain.Enums;

namespace FinancialTransaction.IntegrationTests.Api;

public class TransactionEndpointsTests : IClassFixture<TransactionsApiFixture>
{
    private readonly TransactionsApiFixture _fixture;

    public TransactionEndpointsTests(TransactionsApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task POST_com_contas_validas_cria_transacao_pendente_e_retorna_201()
    {
        var (sourceId, destinationId) = await SeedAccountsAsync();
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/transactions",
            new CreateTransactionRequest(sourceId, destinationId, 250.50m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>();

        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.Equal(nameof(TransactionStatus.Pending), body.Status);
        Assert.Equal(response.Headers.Location, new Uri($"/api/transactions/{body.Id}", UriKind.Relative));
    }

    [Fact]
    public async Task POST_com_conta_inexistente_retorna_404()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/transactions",
            new CreateTransactionRequest(Guid.NewGuid(), Guid.NewGuid(), 100m));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task POST_com_valor_invalido_retorna_400()
    {
        var (sourceId, destinationId) = await SeedAccountsAsync();
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/transactions",
            new CreateTransactionRequest(sourceId, destinationId, 0m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GET_por_id_existente_retorna_200_com_transacao()
    {
        var (sourceId, destinationId) = await SeedAccountsAsync();
        var client = _fixture.CreateClient();
        var created = await client.PostAsJsonAsync(
            "/api/transactions",
            new CreateTransactionRequest(sourceId, destinationId, 75m));
        var createdBody = await created.Content.ReadFromJsonAsync<TransactionResponse>();

        var response = await client.GetAsync($"/api/transactions/{createdBody!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.Equal(createdBody.Id, body!.Id);
    }

    [Fact]
    public async Task GET_por_id_inexistente_retorna_404()
    {
        var client = _fixture.CreateClient();

        var response = await client.GetAsync($"/api/transactions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GET_lista_retorna_200_com_transacoes_criadas()
    {
        var (sourceId, destinationId) = await SeedAccountsAsync();
        var client = _fixture.CreateClient();
        await client.PostAsJsonAsync("/api/transactions", new CreateTransactionRequest(sourceId, destinationId, 30m));

        var response = await client.GetAsync("/api/transactions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>();
        Assert.NotNull(body);
        Assert.NotEmpty(body!);
    }

    private async Task<(Guid SourceId, Guid DestinationId)> SeedAccountsAsync()
    {
        await using var dbContext = _fixture.CreateDbContext();

        var source = Account.Create($"ACC-{Guid.NewGuid():N}");
        var destination = Account.Create($"ACC-{Guid.NewGuid():N}");

        dbContext.Accounts.AddRange(source, destination);
        await dbContext.SaveChangesAsync();

        return (source.Id, destination.Id);
    }
}

using System.Net;
using System.Net.Http.Json;
using FinancialTransaction.Application.Transactions.Dtos;
using FinancialTransaction.Domain.Entities;
using FinancialTransaction.Domain.Enums;

namespace FinancialTransaction.IntegrationTests.EndToEnd;

/// <summary>
/// Testa o fluxo financeiro completo descrito na Fase 9:
///
///   Blazor (simulado pelo HttpClient) -> API -> PostgreSQL -> Kafka -> Worker -> PostgreSQL
///
/// A resposta do POST é síncrona e reflete apenas a persistência inicial (Pending).
/// O processamento real acontece de forma assíncrona: a API publica o evento no Kafka
/// e retorna imediatamente, enquanto o Worker consome a mensagem em segundo plano e
/// atualiza o status no PostgreSQL. Por isso o teste faz polling em GET /api/transactions/{id}
/// até a transação atingir um status final, da mesma forma que o frontend Blazor faz.
/// </summary>
public class FullFlowTests : IClassFixture<FullFlowFixture>
{
    private readonly FullFlowFixture _fixture;

    public FullFlowTests(FullFlowFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Fluxo_completo_cria_transacao_pending_e_worker_a_processa_ate_Processed()
    {
        var (sourceId, destinationId) = await SeedAccountsAsync();
        var client = _fixture.CreateApiClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/transactions",
            new CreateTransactionRequest(sourceId, destinationId, 500m));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(created);
        Assert.Equal(nameof(TransactionStatus.Pending), created!.Status);

        var final = await PollUntilFinalStatusAsync(client, created.Id, TimeSpan.FromSeconds(30));

        Assert.Equal(nameof(TransactionStatus.Processed), final.Status);
        Assert.Null(final.FailureReason);

        await using var dbContext = _fixture.CreateDbContext();
        var persisted = await dbContext.Transactions.FindAsync(created.Id);
        Assert.NotNull(persisted);
        Assert.Equal(TransactionStatus.Processed, persisted!.Status);
    }

    [Fact]
    public async Task Fluxo_completo_com_conta_excluida_apos_publicacao_worker_marca_como_Failed()
    {
        var (sourceId, destinationId) = await SeedAccountsAsync();
        var client = _fixture.CreateApiClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/transactions",
            new CreateTransactionRequest(sourceId, destinationId, 10m));
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();

        await using (var dbContext = _fixture.CreateDbContext())
        {
            var destination = await dbContext.Accounts.FindAsync(destinationId);
            if (destination is not null)
            {
                dbContext.Accounts.Remove(destination);
                await dbContext.SaveChangesAsync();
            }
        }

        var final = await PollUntilFinalStatusAsync(client, created!.Id, TimeSpan.FromSeconds(30));

        Assert.Equal(nameof(TransactionStatus.Failed), final.Status);
        Assert.False(string.IsNullOrWhiteSpace(final.FailureReason));
    }

    private static async Task<TransactionResponse> PollUntilFinalStatusAsync(
        HttpClient client, Guid transactionId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);

        while (true)
        {
            var response = await client.GetAsync($"/api/transactions/{transactionId}");
            response.EnsureSuccessStatusCode();

            var current = await response.Content.ReadFromJsonAsync<TransactionResponse>();
            Assert.NotNull(current);

            if (current!.Status is nameof(TransactionStatus.Processed) or nameof(TransactionStatus.Failed))
            {
                return current;
            }

            if (DateTime.UtcNow >= deadline)
            {
                Assert.Fail($"Transação '{transactionId}' não atingiu status final em {timeout.TotalSeconds}s (status atual: {current.Status}).");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }
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

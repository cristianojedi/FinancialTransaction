using System.Net.Http.Json;
using FinancialTransaction.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinancialTransaction.Web.Services;

public sealed class FinancialApiClient(HttpClient httpClient) : IFinancialApiClient
{
    public async Task<IReadOnlyList<AccountResponse>> GetAccountsAsync(CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync("/api/accounts", ct);
        await EnsureSuccessAsync(response, ct);

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<AccountResponse>>(cancellationToken: ct)
            ?? [];
    }

    public async Task<TransactionResponse> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsJsonAsync("/api/transactions", request, ct);
        await EnsureSuccessAsync(response, ct);

        return await response.Content.ReadFromJsonAsync<TransactionResponse>(cancellationToken: ct)
            ?? throw new ApiException("A API não retornou os dados da transação criada.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ProblemDetails? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
        }
        catch (System.Text.Json.JsonException)
        {
            // Corpo da resposta não é um ProblemDetails válido; usa mensagem genérica abaixo.
        }

        var message = problem?.Detail ?? problem?.Title ?? $"Falha ao comunicar com a API (HTTP {(int)response.StatusCode}).";

        throw new ApiException(message);
    }
}

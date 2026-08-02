using FinancialTransaction.Application.Transactions;
using FinancialTransaction.Application.Transactions.Dtos;

namespace FinancialTransaction.Api.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transactions").WithTags("Transactions");

        group.MapPost("", CreateTransactionAsync)
            .WithName("CreateTransaction")
            .WithSummary("Cria uma nova transação financeira como Pending")
            .Produces<TransactionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}", GetTransactionByIdAsync)
            .WithName("GetTransactionById")
            .WithSummary("Consulta uma transação financeira pelo Id")
            .Produces<TransactionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("", GetTransactionsAsync)
            .WithName("GetTransactions")
            .WithSummary("Lista todas as transações financeiras")
            .Produces<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK);

        group.MapDelete("/{id:guid}", DeleteTransactionAsync)
            .WithName("DeleteTransaction")
            .WithSummary("Exclui uma transação financeira pelo Id")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateTransactionAsync(
        CreateTransactionRequest request,
        ITransactionService transactionService,
        CancellationToken cancellationToken)
    {
        var response = await transactionService.CreateAsync(request, cancellationToken);

        return Results.Created($"/api/transactions/{response.Id}", response);
    }

    private static async Task<IResult> GetTransactionByIdAsync(
        Guid id,
        ITransactionService transactionService,
        CancellationToken cancellationToken)
    {
        var response = await transactionService.GetByIdAsync(id, cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> GetTransactionsAsync(
        ITransactionService transactionService,
        CancellationToken cancellationToken)
    {
        var response = await transactionService.GetAllAsync(cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> DeleteTransactionAsync(
        Guid id,
        ITransactionService transactionService,
        CancellationToken cancellationToken)
    {
        await transactionService.DeleteAsync(id, cancellationToken);

        return Results.NoContent();
    }
}

using FinancialTransaction.Application.Accounts;
using FinancialTransaction.Application.Accounts.Dtos;

namespace FinancialTransaction.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts").WithTags("Accounts");

        group.MapGet("", GetAccountsAsync)
            .WithName("GetAccounts")
            .WithSummary("Lista todas as contas cadastradas")
            .Produces<IReadOnlyList<AccountResponse>>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<IResult> GetAccountsAsync(
        IAccountService accountService,
        CancellationToken cancellationToken)
    {
        var response = await accountService.GetAllAsync(cancellationToken);

        return Results.Ok(response);
    }
}

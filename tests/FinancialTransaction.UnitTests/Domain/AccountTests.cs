using FinancialTransaction.Domain.Entities;

namespace FinancialTransaction.UnitTests.Domain;

public class AccountTests
{
    [Fact]
    public void Create_DeveCriarConta_QuandoNumeroForValido()
    {
        var account = Account.Create("ACC-001");

        Assert.Equal("ACC-001", account.Number);
        Assert.NotEqual(Guid.Empty, account.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_DeveLancarExcecao_QuandoNumeroForInvalido(string? number)
    {
        var act = () => Account.Create(number!);

        Assert.Throws<ArgumentException>(act);
    }
}

using FinancialTransaction.Domain.Enums;
using FinancialTransaction.Domain.Exceptions;
using DomainEntities = FinancialTransaction.Domain.Entities;

namespace FinancialTransaction.UnitTests.Domain;

public class FinancialTransactionTests
{
    [Fact]
    public void Create_DeveLancarExcecao_QuandoValorNaoForMaiorQueZero()
    {
        var sourceAccountId = Guid.NewGuid();
        var destinationAccountId = Guid.NewGuid();

        var act = () => DomainEntities.FinancialTransaction.Create(sourceAccountId, destinationAccountId, 0m);

        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Create_DeveLancarExcecao_QuandoContaOrigemForIgualContaDestino()
    {
        var accountId = Guid.NewGuid();

        var act = () => DomainEntities.FinancialTransaction.Create(accountId, accountId, 100m);

        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Create_DeveIniciarComoPending()
    {
        var transaction = DomainEntities.FinancialTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);

        Assert.Equal(TransactionStatus.Pending, transaction.Status);
    }

    [Fact]
    public void StartProcessing_DeveTransicionarDePendingParaProcessing()
    {
        var transaction = DomainEntities.FinancialTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);

        transaction.StartProcessing();

        Assert.Equal(TransactionStatus.Processing, transaction.Status);
    }

    [Fact]
    public void CompleteProcessing_DeveTransicionarDeProcessingParaProcessed()
    {
        var transaction = DomainEntities.FinancialTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        transaction.StartProcessing();

        transaction.CompleteProcessing();

        Assert.Equal(TransactionStatus.Processed, transaction.Status);
    }

    [Fact]
    public void FailProcessing_DeveTransicionarDeProcessingParaFailed()
    {
        var transaction = DomainEntities.FinancialTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        transaction.StartProcessing();

        transaction.FailProcessing("Saldo insuficiente");

        Assert.Equal(TransactionStatus.Failed, transaction.Status);
        Assert.Equal("Saldo insuficiente", transaction.FailureReason);
    }

    [Fact]
    public void StartProcessing_DeveLancarExcecao_QuandoTransacaoNaoEstiverPending()
    {
        var transaction = DomainEntities.FinancialTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        transaction.StartProcessing();

        void act() => transaction.StartProcessing();

        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void CompleteProcessing_DeveLancarExcecao_QuandoTransacaoNaoEstiverProcessing()
    {
        var transaction = DomainEntities.FinancialTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);

        void act() => transaction.CompleteProcessing();

        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void FailProcessing_DeveLancarExcecao_QuandoTransacaoNaoEstiverProcessing()
    {
        var transaction = DomainEntities.FinancialTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);

        void act() => transaction.FailProcessing("motivo qualquer");

        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void EstadosFinais_NaoDevemVoltarParaPending()
    {
        var processed = DomainEntities.FinancialTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        processed.StartProcessing();
        processed.CompleteProcessing();

        Assert.Throws<DomainException>(() => processed.StartProcessing());

        var failed = DomainEntities.FinancialTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m);
        failed.StartProcessing();
        failed.FailProcessing("erro");

        Assert.Throws<DomainException>(() => failed.StartProcessing());
    }
}

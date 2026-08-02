using FinancialTransaction.Application.Common.Exceptions;
using FinancialTransaction.Application.Transactions;
using FinancialTransaction.Domain.Entities;
using FinancialTransaction.Domain.Enums;
using FinancialTransaction.UnitTests.Application.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinancialTransaction.UnitTests.Application;

public class TransactionProcessingServiceTests
{
    private readonly InMemoryAccountRepository _accountRepository = new();
    private readonly InMemoryFinancialTransactionRepository _transactionRepository = new();
    private readonly ITransactionProcessingService _sut;

    public TransactionProcessingServiceTests()
    {
        _sut = new TransactionProcessingService(
            _transactionRepository,
            _accountRepository,
            new NoOpUnitOfWork(),
            NullLogger<TransactionProcessingService>.Instance);
    }

    [Fact]
    public async Task ProcessAsync_com_contas_validas_marca_transacao_como_processed()
    {
        var source = Account.Create("ACC-001");
        var destination = Account.Create("ACC-002");
        await _accountRepository.AddAsync(source);
        await _accountRepository.AddAsync(destination);
        var transaction = FinancialTransaction.Domain.Entities.FinancialTransaction.Create(source.Id, destination.Id, 100m);
        await _transactionRepository.AddAsync(transaction);

        await _sut.ProcessAsync(transaction.Id);

        var updated = await _transactionRepository.GetByIdAsync(transaction.Id);
        Assert.Equal(TransactionStatus.Processed, updated!.Status);
    }

    [Fact]
    public async Task ProcessAsync_com_conta_origem_inexistente_marca_transacao_como_failed()
    {
        var destination = Account.Create("ACC-002");
        await _accountRepository.AddAsync(destination);
        var transaction = FinancialTransaction.Domain.Entities.FinancialTransaction.Create(Guid.NewGuid(), destination.Id, 100m);
        await _transactionRepository.AddAsync(transaction);

        await _sut.ProcessAsync(transaction.Id);

        var updated = await _transactionRepository.GetByIdAsync(transaction.Id);
        Assert.Equal(TransactionStatus.Failed, updated!.Status);
        Assert.False(string.IsNullOrWhiteSpace(updated.FailureReason));
    }

    [Fact]
    public async Task ProcessAsync_com_transacao_inexistente_lanca_NotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.ProcessAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ProcessAsync_com_transacao_ja_processada_nao_altera_estado()
    {
        var source = Account.Create("ACC-001");
        var destination = Account.Create("ACC-002");
        await _accountRepository.AddAsync(source);
        await _accountRepository.AddAsync(destination);
        var transaction = FinancialTransaction.Domain.Entities.FinancialTransaction.Create(source.Id, destination.Id, 100m);
        transaction.StartProcessing();
        transaction.CompleteProcessing();
        await _transactionRepository.AddAsync(transaction);

        await _sut.ProcessAsync(transaction.Id);

        var updated = await _transactionRepository.GetByIdAsync(transaction.Id);
        Assert.Equal(TransactionStatus.Processed, updated!.Status);
    }

    [Fact]
    public async Task ProcessAsync_com_transacao_ja_em_processamento_retoma_e_conclui()
    {
        var source = Account.Create("ACC-001");
        var destination = Account.Create("ACC-002");
        await _accountRepository.AddAsync(source);
        await _accountRepository.AddAsync(destination);
        var transaction = FinancialTransaction.Domain.Entities.FinancialTransaction.Create(source.Id, destination.Id, 100m);
        transaction.StartProcessing();
        await _transactionRepository.AddAsync(transaction);

        await _sut.ProcessAsync(transaction.Id);

        var updated = await _transactionRepository.GetByIdAsync(transaction.Id);
        Assert.Equal(TransactionStatus.Processed, updated!.Status);
    }
}

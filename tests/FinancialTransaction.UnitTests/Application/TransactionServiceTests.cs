using FinancialTransaction.Application.Common.Exceptions;
using FinancialTransaction.Application.Transactions;
using FinancialTransaction.Application.Transactions.Dtos;
using FinancialTransaction.Domain.Entities;
using FinancialTransaction.Domain.Enums;
using FinancialTransaction.Domain.Events;
using FinancialTransaction.Domain.Exceptions;
using FinancialTransaction.UnitTests.Application.Fakes;

namespace FinancialTransaction.UnitTests.Application;

public class TransactionServiceTests
{
    private readonly InMemoryAccountRepository _accountRepository = new();
    private readonly InMemoryFinancialTransactionRepository _transactionRepository = new();
    private readonly NoOpEventPublisher _eventPublisher = new();
    private readonly ITransactionService _sut;

    public TransactionServiceTests()
    {
        _sut = new TransactionService(_accountRepository, _transactionRepository, new NoOpUnitOfWork(), _eventPublisher);
    }

    [Fact]
    public async Task CreateAsync_com_contas_validas_persiste_transacao_como_pending()
    {
        var source = Account.Create("ACC-001");
        var destination = Account.Create("ACC-002");
        await _accountRepository.AddAsync(source);
        await _accountRepository.AddAsync(destination);

        var response = await _sut.CreateAsync(new CreateTransactionRequest(source.Id, destination.Id, 150m));

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(source.Id, response.SourceAccountId);
        Assert.Equal(destination.Id, response.DestinationAccountId);
        Assert.Equal(150m, response.Amount);
        Assert.Equal(nameof(TransactionStatus.Pending), response.Status);
        Assert.NotNull(await _transactionRepository.GetByIdAsync(response.Id));
    }

    [Fact]
    public async Task CreateAsync_com_contas_validas_publica_evento_TransactionCreated()
    {
        var source = Account.Create("ACC-001");
        var destination = Account.Create("ACC-002");
        await _accountRepository.AddAsync(source);
        await _accountRepository.AddAsync(destination);

        var response = await _sut.CreateAsync(new CreateTransactionRequest(source.Id, destination.Id, 150m));

        var publishedEvent = Assert.Single(_eventPublisher.PublishedEvents);
        var transactionCreated = Assert.IsType<TransactionCreated>(publishedEvent);
        Assert.Equal(response.Id, transactionCreated.TransactionId);
        Assert.Equal(source.Id, transactionCreated.SourceAccountId);
        Assert.Equal(destination.Id, transactionCreated.DestinationAccountId);
        Assert.Equal(150m, transactionCreated.Amount);
    }

    [Fact]
    public async Task CreateAsync_com_conta_origem_inexistente_lanca_NotFoundException()
    {
        var destination = Account.Create("ACC-002");
        await _accountRepository.AddAsync(destination);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.CreateAsync(new CreateTransactionRequest(Guid.NewGuid(), destination.Id, 100m)));
    }

    [Fact]
    public async Task CreateAsync_com_conta_destino_inexistente_lanca_NotFoundException()
    {
        var source = Account.Create("ACC-001");
        await _accountRepository.AddAsync(source);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.CreateAsync(new CreateTransactionRequest(source.Id, Guid.NewGuid(), 100m)));
    }

    [Fact]
    public async Task CreateAsync_com_valor_invalido_lanca_DomainException()
    {
        var source = Account.Create("ACC-001");
        var destination = Account.Create("ACC-002");
        await _accountRepository.AddAsync(source);
        await _accountRepository.AddAsync(destination);

        await Assert.ThrowsAsync<DomainException>(() =>
            _sut.CreateAsync(new CreateTransactionRequest(source.Id, destination.Id, 0m)));
    }

    [Fact]
    public async Task CreateAsync_com_contas_iguais_lanca_DomainException()
    {
        var account = Account.Create("ACC-001");
        await _accountRepository.AddAsync(account);

        await Assert.ThrowsAsync<DomainException>(() =>
            _sut.CreateAsync(new CreateTransactionRequest(account.Id, account.Id, 100m)));
    }

    [Fact]
    public async Task GetByIdAsync_com_id_inexistente_lanca_NotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdAsync_com_id_existente_retorna_transacao()
    {
        var source = Account.Create("ACC-001");
        var destination = Account.Create("ACC-002");
        await _accountRepository.AddAsync(source);
        await _accountRepository.AddAsync(destination);
        var created = await _sut.CreateAsync(new CreateTransactionRequest(source.Id, destination.Id, 50m));

        var result = await _sut.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
    }

    [Fact]
    public async Task GetAllAsync_retorna_todas_as_transacoes_criadas()
    {
        var source = Account.Create("ACC-001");
        var destination = Account.Create("ACC-002");
        await _accountRepository.AddAsync(source);
        await _accountRepository.AddAsync(destination);
        await _sut.CreateAsync(new CreateTransactionRequest(source.Id, destination.Id, 10m));
        await _sut.CreateAsync(new CreateTransactionRequest(source.Id, destination.Id, 20m));

        var result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task DeleteAsync_com_id_existente_remove_transacao()
    {
        var source = Account.Create("ACC-001");
        var destination = Account.Create("ACC-002");
        await _accountRepository.AddAsync(source);
        await _accountRepository.AddAsync(destination);
        var created = await _sut.CreateAsync(new CreateTransactionRequest(source.Id, destination.Id, 10m));

        await _sut.DeleteAsync(created.Id);

        Assert.Null(await _transactionRepository.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task DeleteAsync_com_id_inexistente_lanca_NotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }
}

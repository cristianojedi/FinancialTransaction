using System.Diagnostics;
using FinancialTransaction.Application.Abstractions.Messaging;
using FinancialTransaction.Application.Abstractions.Persistence;
using FinancialTransaction.Application.Common.Exceptions;
using FinancialTransaction.Application.Common.Telemetry;
using FinancialTransaction.Application.Transactions.Dtos;

namespace FinancialTransaction.Application.Transactions;

public class TransactionService : ITransactionService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IFinancialTransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public TransactionService(
        IAccountRepository accountRepository,
        IFinancialTransactionRepository transactionRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<TransactionResponse> CreateAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity(
            "TransactionService.CreateAsync",
            ActivityKind.Internal);

        activity?.SetTag("transaction.source_account_id", request.SourceAccountId);
        activity?.SetTag("transaction.destination_account_id", request.DestinationAccountId);
        activity?.SetTag("transaction.amount", request.Amount);

        var sourceAccount = await _accountRepository.GetByIdAsync(request.SourceAccountId, cancellationToken)
            ?? throw new NotFoundException($"Conta de origem '{request.SourceAccountId}' não encontrada.");

        var destinationAccount = await _accountRepository.GetByIdAsync(request.DestinationAccountId, cancellationToken)
            ?? throw new NotFoundException($"Conta de destino '{request.DestinationAccountId}' não encontrada.");

        var transaction = Domain.Entities.FinancialTransaction.Create(
            sourceAccount.Id,
            destinationAccount.Id,
            request.Amount);

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in transaction.DomainEvents)
        {
            await _eventPublisher.PublishAsync(domainEvent, cancellationToken);
        }

        transaction.ClearDomainEvents();

        activity?.SetTag("transaction.id", transaction.Id);
        activity?.SetTag("transaction.status", transaction.Status.ToString());

        ApplicationMetrics.TransactionsCreated.Add(1);

        return TransactionResponse.FromDomain(transaction);
    }

    public async Task<TransactionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Transação '{id}' não encontrada.");

        return TransactionResponse.FromDomain(transaction);
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var transactions = await _transactionRepository.GetAllAsync(cancellationToken);

        return transactions.Select(TransactionResponse.FromDomain).ToList();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Transação '{id}' não encontrada.");

        await _transactionRepository.DeleteAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

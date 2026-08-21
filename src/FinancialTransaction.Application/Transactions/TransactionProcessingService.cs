using System.Diagnostics;
using FinancialTransaction.Application.Abstractions.Persistence;
using FinancialTransaction.Application.Common.Exceptions;
using FinancialTransaction.Application.Common.Telemetry;
using FinancialTransaction.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FinancialTransaction.Application.Transactions;

public class TransactionProcessingService : ITransactionProcessingService
{
    private readonly IFinancialTransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionProcessingService> _logger;

    public TransactionProcessingService(
        IFinancialTransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        ILogger<TransactionProcessingService> logger)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        // Span de processamento: filho do span de consumo Kafka quando chamado pelo Worker
        // (Activity.Current fica ambiente), representando a regra de negócio em si.
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity(
            "TransactionProcessingService.ProcessAsync",
            ActivityKind.Internal);

        activity?.SetTag("transaction.id", transactionId);

        var stopwatch = Stopwatch.StartNew();

        var transaction = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken)
            ?? throw new NotFoundException($"Transação '{transactionId}' não encontrada.");

        if (transaction.Status is TransactionStatus.Processed or TransactionStatus.Failed)
        {
            activity?.SetTag("transaction.status", transaction.Status.ToString());

            _logger.LogInformation(
                "Transação {TransactionId} já está no estado final {Status}. Mensagem ignorada.",
                transactionId,
                transaction.Status);

            return;
        }

        if (transaction.Status == TransactionStatus.Pending)
        {
            transaction.StartProcessing();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var sourceAccount = await _accountRepository.GetByIdAsync(transaction.SourceAccountId, cancellationToken);
        var destinationAccount = await _accountRepository.GetByIdAsync(transaction.DestinationAccountId, cancellationToken);

        if (sourceAccount is null || destinationAccount is null)
        {
            transaction.FailProcessing("Conta de origem ou destino não encontrada.");
        }
        else
        {
            transaction.CompleteProcessing();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        activity?.SetTag("transaction.status", transaction.Status.ToString());

        ApplicationMetrics.TransactionsProcessed.Add(1,
            new KeyValuePair<string, object?>("status", transaction.Status.ToString()));
        ApplicationMetrics.TransactionProcessingDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
    }
}

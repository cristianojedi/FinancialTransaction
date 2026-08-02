using System.Text.Json;
using Confluent.Kafka;
using FinancialTransaction.Application.Transactions;
using FinancialTransaction.Domain.Events;
using FinancialTransaction.Infrastructure.Messaging;
using Microsoft.Extensions.Options;

namespace FinancialTransaction.Worker;

public sealed class Worker : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IConsumer<string, string> consumer,
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<Worker> logger)
    {
        _consumer = consumer;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_options.TransactionsTopic);

        _logger.LogInformation(
            "Worker inscrito no tópico {Topic} (consumer group {GroupId}).",
            _options.TransactionsTopic,
            _options.ConsumerGroupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? consumeResult;

                try
                {
                    consumeResult = _consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Falha ao consumir mensagem do Kafka: {Reason}.", ex.Error.Reason);
                    continue;
                }

                if (consumeResult?.Message is null)
                {
                    continue;
                }

                await ProcessMessageAsync(consumeResult, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Encerramento solicitado via stoppingToken.
        }
        finally
        {
            _consumer.Close();
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken)
    {
        try
        {
            var transactionCreated = JsonSerializer.Deserialize<TransactionCreated>(consumeResult.Message.Value)
                ?? throw new InvalidOperationException("Mensagem Kafka vazia ou inválida.");

            using var scope = _scopeFactory.CreateScope();
            var processingService = scope.ServiceProvider.GetRequiredService<ITransactionProcessingService>();

            await processingService.ProcessAsync(transactionCreated.TransactionId, cancellationToken);

            _consumer.Commit(consumeResult);

            _logger.LogInformation(
                "Transação {TransactionId} processada (partition {Partition}, offset {Offset}).",
                transactionCreated.TransactionId,
                consumeResult.Partition.Value,
                consumeResult.Offset.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao processar mensagem (partition {Partition}, offset {Offset}). Offset não será commitado; a mensagem será reprocessada.",
                consumeResult.Partition.Value,
                consumeResult.Offset.Value);
        }
    }

    public override void Dispose()
    {
        _consumer.Dispose();
        base.Dispose();
    }
}

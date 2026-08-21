using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using FinancialTransaction.Application.Transactions;
using FinancialTransaction.Domain.Events;
using FinancialTransaction.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

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
        // Extract: lê o traceparent (e o baggage, se houver) dos headers Kafka gravados pelo Producer.
        // Se a mensagem não trouxer traceparent (ex.: publicada antes desta fase), ActivityContext fica default
        // e o StartActivity abaixo se comporta como antes, iniciando um trace novo.
        var propagationContext = Propagators.DefaultTextMapPropagator.Extract(
            default,
            consumeResult.Message.Headers,
            ExtractHeaderValues);

        Baggage.Current = propagationContext.Baggage;

        // Span de consumo: continua o trace da API através do contexto extraído dos headers Kafka.
        // ActivityKind.Consumer sinaliza que esta operação recebe uma mensagem de um sistema de mensageria.
        using var activity = WorkerDiagnostics.ActivitySource.StartActivity(
            $"{_options.TransactionsTopic} consume",
            ActivityKind.Consumer,
            propagationContext.ActivityContext);

        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination", consumeResult.Topic);
        activity?.SetTag("messaging.kafka.consumer_group", _options.ConsumerGroupId);
        activity?.SetTag("messaging.kafka.partition", consumeResult.Partition.Value);
        activity?.SetTag("messaging.kafka.offset", consumeResult.Offset.Value);
        activity?.SetTag("messaging.kafka.message_key", consumeResult.Message.Key);

        var stopwatch = Stopwatch.StartNew();

        WorkerMetrics.KafkaMessagesConsumed.Add(1,
            new KeyValuePair<string, object?>("topic", consumeResult.Topic));

        try
        {
            var transactionCreated = JsonSerializer.Deserialize<TransactionCreated>(consumeResult.Message.Value)
                ?? throw new InvalidOperationException("Mensagem Kafka vazia ou inválida.");

            activity?.SetTag("transaction.id", transactionCreated.TransactionId);

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
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            _logger.LogError(
                ex,
                "Falha ao processar mensagem (partition {Partition}, offset {Offset}). Offset não será commitado; a mensagem será reprocessada.",
                consumeResult.Partition.Value,
                consumeResult.Offset.Value);

            WorkerMetrics.ProcessingErrors.Add(1,
                new KeyValuePair<string, object?>("topic", consumeResult.Topic));
        }
        finally
        {
            WorkerMetrics.MessageProcessingDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private static IEnumerable<string> ExtractHeaderValues(Headers headers, string key)
    {
        if (headers.TryGetLastBytes(key, out var value))
        {
            yield return Encoding.UTF8.GetString(value);
        }
    }

    public override void Dispose()
    {
        _consumer.Dispose();
        base.Dispose();
    }
}

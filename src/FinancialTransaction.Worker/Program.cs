using FinancialTransaction.Application;
using FinancialTransaction.Application.Common.Telemetry;
using FinancialTransaction.Infrastructure;
using FinancialTransaction.Worker;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

const string ServiceName = "FinancialTransaction.Worker";

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<Worker>();

// OpenTelemetry — instrumentação SOMENTE deste Worker (consumo Kafka, processamento da transação e EF Core/PostgreSQL).
// Trace independente do trace da API nesta fase: ainda não há propagação de contexto através do Kafka.
// Sem Collector/Jaeger: os traces são exportados para o Console para validação local.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: ServiceName))
    .WithTracing(tracing => tracing
        .AddSource(WorkerDiagnostics.SourceName)
        .AddSource(ApplicationDiagnostics.SourceName)
        .AddEntityFrameworkCoreInstrumentation()
        .AddConsoleExporter());

var host = builder.Build();
host.Run();

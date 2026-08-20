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

// OpenTelemetry — instrumentação deste Worker (consumo Kafka, processamento da transação e EF Core/PostgreSQL).
// O span de consumo (WorkerDiagnostics) extrai o traceparent dos headers Kafka gravados pelo Producer da API,
// continuando o mesmo trace distribuído em vez de iniciar um trace novo. Sem Collector/Jaeger: os traces são
// exportados para o Console para validação local.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: ServiceName))
    .WithTracing(tracing => tracing
        .AddSource(WorkerDiagnostics.SourceName)
        .AddSource(ApplicationDiagnostics.SourceName)
        .AddEntityFrameworkCoreInstrumentation()
        .AddConsoleExporter());

var host = builder.Build();
host.Run();

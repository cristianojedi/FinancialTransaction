# Observabilidade — Fase 10: OpenTelemetry incremental na API

> Esta fase instrumenta **somente** a `FinancialTransaction.Api`. O Worker, a propagação de contexto pelo Kafka, o
> OpenTelemetry Collector, Prometheus, Grafana e Jaeger/Tempo ficam para fases posteriores (11 a 14).

## 1. Conceitos

### 1.1 Observabilidade

Observabilidade é a capacidade de responder perguntas sobre o comportamento interno de um sistema **a partir dos
dados que ele emite de fora para dentro** (traces, métricas e logs), sem precisar alterar o código para investigar
cada novo problema. Diferente de monitoramento tradicional (que verifica sintomas conhecidos, como "a CPU está
alta?"), observabilidade permite investigar perguntas que você não previu, como "por que **esta** transação
específica demorou 2 segundos?".

### 1.2 Telemetria

Telemetria é o conjunto de dados coletados sobre a execução de um sistema e enviados para fora dele: traces,
métricas e logs. É a matéria-prima da observabilidade.

### 1.3 OpenTelemetry

OpenTelemetry (OTel) é um padrão aberto (CNCF) — composto por uma especificação, APIs, SDKs e um formato de
transporte (OTLP) — para gerar, coletar e exportar telemetria de forma vendor-neutral. Ou seja, a aplicação não
fica acoplada ao Jaeger, ao Grafana ou a qualquer backend específico: instrumenta-se uma vez com OpenTelemetry e
troca-se o destino da telemetria apenas trocando o *exporter*.

### 1.4 Trace

Um **Trace** representa o caminho completo de uma operação através do sistema — por exemplo, uma requisição HTTP
`POST /api/transactions` do início ao fim, incluindo todas as chamadas internas que ela dispara (banco de dados,
outros serviços, etc.). Um trace é identificado por um **TraceId** único e é composto por um ou mais **Spans**.

### 1.5 Span

Um **Span** representa uma unidade de trabalho dentro de um trace — uma operação específica com início, fim e
duração. Exemplos nesta fase: o processamento da requisição HTTP, a execução do caso de uso na Application, cada
comando SQL executado no PostgreSQL. Spans podem ter pai (**ParentSpanId**), formando uma árvore que reconstrói a
linha do tempo da operação.

### 1.6 TraceId

Identificador único (128 bits) de um trace inteiro. Todos os spans que fazem parte da mesma operação distribuída
compartilham o mesmo TraceId. É o valor que se usa para "puxar o fio" e ver tudo o que aconteceu numa requisição.

### 1.7 SpanId

Identificador único (64 bits) de um span específico dentro de um trace. Combinado com o `ParentSpanId`, permite
remontar a hierarquia de chamadas (quem chamou quem).

### 1.8 Activity no ecossistema .NET

`System.Diagnostics.Activity` é a implementação nativa do .NET para o conceito de Span do OpenTelemetry — o .NET
adotou os conceitos de tracing distribuído na própria BCL antes mesmo do OpenTelemetry existir como projeto CNCF.
Um `ActivitySource` cria `Activity`, que é iniciada com `StartActivity(...)` e finalizada automaticamente ao sair
do `using`. O OpenTelemetry SDK para .NET "escuta" as `Activity` criadas (tanto pelas bibliotecas instrumentadas
automaticamente quanto pelas criadas manualmente) e as transforma em Spans exportáveis.

### 1.9 Instrumentação automática x manual

- **Automática**: pacotes de instrumentação (`OpenTelemetry.Instrumentation.AspNetCore`,
  `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Instrumentation.EntityFrameworkCore`) se conectam a pontos
  de extensão já existentes no ASP.NET Core, no `HttpClient` e no EF Core (via `DiagnosticSource`) e geram spans
  sozinhos, sem que o desenvolvedor escreva código de tracing. Cobre o "de fora": requisição HTTP recebida,
  chamadas HTTP de saída, comandos SQL executados.
- **Manual**: o desenvolvedor cria explicitamente uma `Activity` (via `ActivitySource.StartActivity(...)`) para
  representar uma unidade de trabalho que só ele conhece — nesta fase, o caso de uso
  `TransactionService.CreateAsync`, que representa a regra de negócio da Application, algo que nenhuma
  instrumentação automática enxergaria sozinha.

### 1.10 Como o OpenTelemetry coleta os dados

1. Cada `Activity`/Span criado (automático ou manual) é processado pelo `TracerProvider` configurado na aplicação.
2. O `TracerProvider` aplica um `Sampler` (decide se aquele trace deve ser coletado — nesta fase, o padrão
   `AlwaysOn`, ou seja, tudo é coletado).
3. Os spans finalizados passam por um `Processor` (nesta fase, o processor padrão em modo *simple/batch* do
   próprio SDK) e são entregues a um ou mais **Exporters**.
4. O **Exporter** é responsável por serializar e enviar os spans para um destino. Nesta fase usamos o
   `ConsoleExporter`, que apenas imprime os spans no console — não há Collector nem backend de tracing ainda.

### 1.11 OTLP

OTLP (OpenTelemetry Protocol) é o protocolo padrão (gRPC ou HTTP/protobuf) usado para transportar telemetria entre
uma aplicação instrumentada e um backend ou Collector. Ele existe para desacoplar "quem gera" telemetria de "quem
recebe": qualquer aplicação instrumentada com OpenTelemetry pode falar OTLP com qualquer backend compatível
(Jaeger, Tempo, Collector, etc.). **Nesta fase não usamos OTLP** — o exporter é o `ConsoleExporter`, propositalmente
mais simples, para isolar a instrumentação da infraestrutura de observabilidade (que só chega na Fase 13).

## 2. O que foi implementado

Escopo: **somente `FinancialTransaction.Api`**.

```text
Blazor
  │
  ▼
FinancialTransaction.Api
  │
  ├── HTTP Span            (OpenTelemetry.Instrumentation.AspNetCore — automático)
  │
  ├── Application Span     (ActivitySource manual em TransactionService.CreateAsync)
  │
  └── PostgreSQL Span      (OpenTelemetry.Instrumentation.EntityFrameworkCore — automático)
```

### 2.1 Pacotes utilizados (`FinancialTransaction.Api.csproj`)

| Pacote | Versão | Papel |
|---|---|---|
| `OpenTelemetry.Extensions.Hosting` | 1.17.0 | Integra o OpenTelemetry SDK com `IServiceCollection`/`IHostBuilder` (`AddOpenTelemetry()`). |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.17.0 | Instrumentação automática do pipeline HTTP do ASP.NET Core (gera o span `Server` por requisição). |
| `OpenTelemetry.Instrumentation.Http` | 1.17.0 | Instrumentação automática de chamadas de saída feitas via `HttpClient`. Não há chamadas de saída nesta fase, mas já deixa a API pronta para consumir outros serviços HTTP de forma observável. |
| `OpenTelemetry.Instrumentation.EntityFrameworkCore` | 1.17.0-beta.1 | Instrumentação automática dos comandos executados pelo EF Core (gera o span `Client` por comando SQL contra o PostgreSQL). Ainda em beta no NuGet, mas é o pacote oficial mantido pelo projeto OpenTelemetry .NET Contrib. |
| `OpenTelemetry.Exporter.Console` | 1.17.0 | Exporta os spans finalizados para a saída padrão (console) do processo da API. |

### 2.2 Configuração em `Program.cs`

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: ServiceName))
    .WithTracing(tracing => tracing
        .AddSource(ServiceName)                          // spans manuais desta API (reservado)
        .AddSource(ApplicationDiagnostics.SourceName)     // spans manuais da camada Application
        .AddAspNetCoreInstrumentation()                   // HTTP Span
        .AddHttpClientInstrumentation()                   // HttpClient de saída (não usado ainda)
        .AddEntityFrameworkCoreInstrumentation()           // PostgreSQL Span
        .AddConsoleExporter());
```

Explicação de cada configuração:

- **`AddOpenTelemetry()`**: registra o `TracerProvider` (e, futuramente, `MeterProvider`/`LoggerProvider`) no
  container de DI, gerenciado pelo ciclo de vida do host (é inicializado no start e finalizado no shutdown, o que
  garante *flush* dos spans pendentes ao encerrar a aplicação).
- **`ConfigureResource(...).AddService(serviceName: "FinancialTransaction.Api")`**: define o **Resource**, um
  conjunto de atributos que identifica *quem* gerou a telemetria. Sem isso, ao correlacionar múltiplos serviços no
  futuro (API + Worker) seria impossível saber de qual processo cada span veio.
- **`AddSource(ServiceName)` / `AddSource(ApplicationDiagnostics.SourceName)`**: o SDK do OpenTelemetry, por
  padrão, **ignora** qualquer `ActivitySource` que não tenha sido explicitamente registrada — isso evita capturar
  telemetria de bibliotecas não relacionadas. Como a Application cria spans manuais através do
  `ActivitySource` `"FinancialTransaction.Application"` (ver `ApplicationDiagnostics`), ele precisa ser
  registrado aqui para que esses spans sejam exportados.
- **`AddAspNetCoreInstrumentation()`**: liga a instrumentação automática do pipeline HTTP — cada requisição recebida
  vira um span `Kind = Server`.
- **`AddHttpClientInstrumentation()`**: liga a instrumentação automática de chamadas HTTP de saída via
  `HttpClient` (ainda sem uso nesta fase, mas correto instrumentar desde já, pois a API expõe integrações HTTP por
  natureza).
- **`AddEntityFrameworkCoreInstrumentation()`**: liga a instrumentação automática do EF Core — cada comando SQL
  executado pelo `FinancialTransactionDbContext` vira um span `Kind = Client` com atributos como `db.system`,
  `db.name` e `db.statement`.
- **`AddConsoleExporter()`**: define o destino da telemetria. Nesta fase, propositalmente, é apenas o console do
  processo — sem Collector, sem Jaeger. Isso é suficiente para provar que a instrumentação está funcionando antes
  de introduzir qualquer peça de infraestrutura adicional.

### 2.3 Span manual da Application

Arquivo: [ApplicationDiagnostics.cs](../src/FinancialTransaction.Application/Common/Telemetry/ApplicationDiagnostics.cs)

```csharp
public static class ApplicationDiagnostics
{
    public const string SourceName = "FinancialTransaction.Application";
    public static readonly ActivitySource ActivitySource = new(SourceName);
}
```

Uso em [TransactionService.CreateAsync](../src/FinancialTransaction.Application/Transactions/TransactionService.cs):

```csharp
using var activity = ApplicationDiagnostics.ActivitySource.StartActivity(
    "TransactionService.CreateAsync",
    ActivityKind.Internal);

activity?.SetTag("transaction.source_account_id", request.SourceAccountId);
activity?.SetTag("transaction.destination_account_id", request.DestinationAccountId);
activity?.SetTag("transaction.amount", request.Amount);
// ... regra de negócio + persistência ...
activity?.SetTag("transaction.id", transaction.Id);
activity?.SetTag("transaction.status", transaction.Status.ToString());
```

Esse span envolve toda a regra de negócio do caso de uso (busca das contas, criação da transação, persistência e
publicação do evento no Kafka), e por estar ativo durante as chamadas ao `DbContext`, os spans do EF Core aparecem
como **filhos** dele automaticamente — é assim que `Activity.Current` funciona no .NET: toda nova `Activity`
criada dentro do escopo de outra vira filha dela por padrão.

### 2.4 TraceId disponível na resposta HTTP

Como ainda não há Jaeger/Grafana para consultar visualmente, foi adicionado um middleware simples que expõe o
TraceId da requisição atual no header `X-Trace-Id` da resposta:

```csharp
app.Use(async (context, next) =>
{
    var traceId = Activity.Current?.TraceId.ToString();
    if (traceId is not null)
    {
        context.Response.Headers["X-Trace-Id"] = traceId;
    }

    await next();
});
```

Isso permite, no Swagger/Postman/curl, pegar o TraceId de qualquer chamada e localizá-lo diretamente no log do
console da API.

## 3. Como validar que um trace foi criado

### 3.1 Subir a infraestrutura e a API

```bash
docker compose up -d
dotnet run --project src/FinancialTransaction.Api
```

### 3.2 Executar uma transação

```bash
curl -i -X POST http://localhost:5209/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"sourceAccountId":"<id-conta-origem>","destinationAccountId":"<id-conta-destino>","amount":150.00}'
```

A resposta trará o header:

```text
X-Trace-Id: 4b807f7234b0394c562b8b940b000600
```

### 3.3 Localizar o trace no console da API

No console onde a API está rodando, procure por esse mesmo `TraceId`. Exemplo real capturado durante esta
implementação:

```text
Activity.TraceId:            4b807f7234b0394c562b8b940b000600
Activity.SpanId:             d3906c9219204a40
Activity.DisplayName:        POST /api/transactions/
Activity.Kind:               Server
Activity.Duration:           00:00:01.4767079
Instrumentation scope (ActivitySource): Microsoft.AspNetCore

Activity.TraceId:            4b807f7234b0394c562b8b940b000600
Activity.SpanId:             ac3a69e2a390cd49
Activity.ParentSpanId:       d3906c9219204a40
Activity.DisplayName:        TransactionService.CreateAsync
Activity.Kind:               Internal
Instrumentation scope (ActivitySource): FinancialTransaction.Application

Activity.TraceId:            4b807f7234b0394c562b8b940b000600
Activity.SpanId:             eaa3ff0d4baea2e3
Activity.ParentSpanId:       ac3a69e2a390cd49
Activity.DisplayName:        financialtransaction
Activity.Kind:               Client
Activity.Tags: db.system: postgresql, db.statement: SELECT ... FROM accounts ...
Instrumentation scope (ActivitySource): OpenTelemetry.Instrumentation.EntityFrameworkCore

Activity.TraceId:            4b807f7234b0394c562b8b940b000600
Activity.SpanId:             124afa0720b185d2
Activity.ParentSpanId:       ac3a69e2a390cd49
Activity.Tags: db.statement: SELECT ... FROM accounts WHERE "Id" = @id ...

Activity.TraceId:            4b807f7234b0394c562b8b940b000600
Activity.SpanId:             d21d92b8fa04664f
Activity.ParentSpanId:       ac3a69e2a390cd49
Activity.Tags: db.statement: INSERT INTO transactions (...) VALUES (...)
```

### 3.4 Como identificar HTTP e PostgreSQL dentro do trace

Todos os spans acima compartilham o **mesmo `Activity.TraceId`** — isso comprova que fazem parte da mesma
operação distribuída (uma única requisição `POST /api/transactions`). Para diferenciar a origem de cada span:

- **Instrumentation scope**: identifica qual pacote gerou o span.
  - `Microsoft.AspNetCore` → span HTTP (a requisição recebida).
  - `FinancialTransaction.Application` → span manual do caso de uso.
  - `OpenTelemetry.Instrumentation.EntityFrameworkCore` → span de acesso ao PostgreSQL.
- **`Activity.Kind`**: `Server` para a requisição HTTP recebida, `Internal` para o processamento de negócio,
  `Client` para as chamadas ao banco (do ponto de vista da API, o PostgreSQL é um recurso externo).
- **`Activity.Tags`**: nos spans do EF Core, o atributo `db.statement` mostra exatamente qual comando SQL foi
  executado (`SELECT`/`INSERT`), e `db.system: postgresql` confirma o motor de banco.
- **Hierarquia via `ParentSpanId`**: o span HTTP (`POST /api/transactions/`, SpanId `d3906c...`) é o pai do span
  da Application (`TransactionService.CreateAsync`, ParentSpanId `d3906c...`), que por sua vez é pai dos três
  spans de banco (todos com `ParentSpanId` apontando para o SpanId da Application). Essa árvore reconstrói
  exatamente o fluxo descrito no objetivo da fase: `HTTP → Application → PostgreSQL`.

## 4. Fora do escopo desta fase (propositalmente)

- OpenTelemetry no `FinancialTransaction.Worker` (Fase 11).
- Propagação de contexto de trace através dos headers do Kafka (Fase 12 — ver
  [distributed-tracing.md](distributed-tracing.md)).
- OpenTelemetry Collector (Fase 13).
- Prometheus, Grafana, Jaeger/Tempo (Fase 14).

A API é observável **de forma independente**: mesmo com Kafka fora do ar, a criação da transação, sua persistência
e o trace HTTP + Application + PostgreSQL continuam funcionando (a publicação do evento é a última etapa do caso
de uso e não afeta os spans já emitidos).

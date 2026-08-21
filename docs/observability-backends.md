# Observabilidade — Fase 14: Jaeger + Prometheus + Grafana

> Escopo desta fase: dar aos sinais que já saíam da Api/Worker (Fase 10-12) e passavam pelo Collector (Fase 13)
> um destino final visualizável — Jaeger para traces, Prometheus para métricas, Grafana como camada única de
> consulta/dashboard sobre os dois. Esta fase também adiciona **métricas** (`WithMetrics`) à Api e ao Worker, que
> até aqui só emitiam traces.

## 1. Conceitos

### 1.1 Backend de tracing

Um backend de tracing é o sistema que **armazena e indexa** spans recebidos (via OTLP ou outro protocolo) e
oferece uma forma de consultá-los — normalmente por `TraceId`, por serviço, por operação ou por intervalo de
tempo/duração. O Collector (Fase 13) processa e encaminha os spans; ele mesmo não armazena nem oferece UI de
consulta. Nesta fase o backend de tracing é o **Jaeger**, que:

- recebe spans diretamente via **OTLP/gRPC** (Jaeger aceita OTLP nativamente desde a v1.35 — não é mais
  necessário um exporter dedicado ao formato Jaeger-Thrift, que está deprecado no ecossistema OpenTelemetry);
- armazena os spans em memória (`all-in-one`, adequado para o laboratório; um ambiente real usaria Cassandra,
  Elasticsearch ou Tempo/object storage);
- expõe uma UI web (`:16686`) para buscar traces por serviço, operação, TraceId ou faixa de duração, e visualizar
  a árvore de spans de cada trace (o "waterfall").

### 1.2 Backend de métricas

Um backend de métricas armazena **séries temporais**: pares (timestamp, valor) identificados por um nome de
métrica e um conjunto de labels (`{job="FinancialTransaction.Api", http_route="/api/transactions/"}`), permitindo
consultas agregadas ao longo do tempo (taxas, percentis, somas). Nesta fase o backend de métricas é o
**Prometheus**, que:

- **não recebe push** de métricas — ele faz *scrape* (pull) periódico de um endpoint HTTP que expõe métricas em
  texto plano no seu formato;
- esse endpoint, aqui, é exposto pelo **próprio OpenTelemetry Collector** (exporter `prometheus`, ver 1.4), não
  pela Api/Worker diretamente — mantendo o mesmo princípio da Fase 13 de a aplicação só conhecer o Collector;
- oferece uma linguagem de consulta, **PromQL**, usada tanto na UI própria (`:9090`) quanto pelos dashboards do
  Grafana.

### 1.3 Grafana

Grafana é uma camada de **visualização e consulta** desacoplada dos backends de dados. Ele mesmo não armazena
telemetria — em vez disso, mantém uma lista de **datasources** (aqui, Prometheus e Jaeger) e, para cada painel de
um dashboard, dispara a consulta correspondente (PromQL para Prometheus, busca por TraceId/serviço para Jaeger) no
datasource configurado. Vantagem central: um único lugar para correlacionar métricas e traces de múltiplos
serviços, sem precisar abrir a UI nativa de cada backend separadamente.

### 1.4 Papel do Collector entre aplicações e backends

O Collector (Fase 13) continua sendo o único destino OTLP conhecido pela Api e pelo Worker. Nesta fase, ele ganha
dois exporters novos, um por tipo de sinal:

```text
Api / Worker
     │ OTLP/HTTP (traces + metrics)
     ▼
OpenTelemetry Collector
     │
     ├── pipeline "traces"  → exporter "otlp/jaeger"   → Jaeger (OTLP/gRPC, :4317 interno)
     └── pipeline "metrics" → exporter "prometheus"    → expõe :8889/metrics (scrape pelo Prometheus)
```

Continua valendo o benefício da Fase 13: trocar Jaeger por Tempo, ou adicionar um segundo backend de métricas, é
uma mudança em `otel-collector-config.yaml` — nenhum código de Api/Worker muda.

### 1.5 Como o Grafana consulta os datasources

Cada painel de um dashboard do Grafana declara um `datasource` (por `uid`) e uma `query`. Ao renderizar (ou em
cada tick do `refresh` automático), o Grafana:

1. Resolve o datasource pelo `uid` (provisionado automaticamente nesta fase — ver
   [datasources.yaml](../infrastructure/docker/observability/grafana/provisioning/datasources/datasources.yaml)
   — sem precisar cadastrar manualmente na UI).
2. Envia a query ao backend correspondente: para o datasource Prometheus, uma expressão PromQL via a API HTTP do
   Prometheus (`/api/v1/query_range`); para o datasource Jaeger, uma busca via a API HTTP do Jaeger
   (`/api/traces`, `/api/traces/{traceID}`).
3. Renderiza o resultado no tipo de painel configurado (série temporal, tabela, trace view).

O Grafana **não duplica dados** — ele é uma janela sobre o Prometheus e o Jaeger, que continuam sendo a fonte da
verdade.

## 2. O que foi implementado

### 2.1 Infraestrutura (`docker-compose.yml`)

Três serviços novos, além da imagem do `otel-collector` trocada de `otel/opentelemetry-collector` (core) para
`otel/opentelemetry-collector-contrib` (o exporter `prometheus` não está na imagem core):

| Serviço | Imagem | Porta host | Papel |
|---|---|---|---|
| `jaeger` | `jaegertracing/all-in-one:1.62.0` | `16686` (UI) | Backend de tracing. `COLLECTOR_OTLP_ENABLED=true` liga o receiver OTLP nativo. |
| `prometheus` | `prom/prometheus:v3.0.1` | `9090` (UI) | Backend de métricas. Faz scrape do Collector a cada 5s (`prometheus.yml`). |
| `grafana` | `grafana/grafana:11.4.0` | `3000` (UI) | Visualização. Login `admin`/`admin`; anônimo habilitado como `Viewer` para facilitar o acesso no laboratório. |

O `otel-collector` ganhou a porta `8889` (endpoint do exporter `prometheus`) e um `depends_on: jaeger` (não é uma
dependência de saúde rígida — apenas ordena a subida).

### 2.2 Collector: pipelines de traces e metrics

[otel-collector-config.yaml](../infrastructure/docker/observability/otel-collector-config.yaml):

```yaml
exporters:
  debug:
    verbosity: basic

  otlp/jaeger:
    endpoint: jaeger:4317
    tls:
      insecure: true

  prometheus:
    endpoint: 0.0.0.0:8889
    resource_to_telemetry_conversion:
      enabled: true

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [resource, batch]
      exporters: [debug, otlp/jaeger]

    metrics:
      receivers: [otlp]
      processors: [resource, batch]
      exporters: [debug, prometheus]
```

`resource_to_telemetry_conversion: enabled: true` faz o exporter `prometheus` transformar os **Resource
Attributes** (`service.name`, `deployment.environment`, etc.) em **labels** de cada série exportada — sem isso,
não seria possível filtrar métricas por serviço (`job="FinancialTransaction.Api"`) no Prometheus/Grafana.

### 2.3 Prometheus: scrape do Collector

[prometheus.yml](../infrastructure/docker/observability/prometheus.yml):

```yaml
scrape_configs:
  - job_name: otel-collector
    honor_labels: true
    static_configs:
      - targets: ["otel-collector:8889"]
```

`honor_labels: true` foi necessário: por padrão, ao encontrar um label `job`/`instance` já presente na métrica
raspada (o `service.name`/`service.instance.id` convertidos pelo Collector, ver 2.2), o Prometheus o renomeia para
`exported_job`/`exported_instance` e sobrescreve `job`/`instance` com os valores do próprio `scrape_config`
(`job="otel-collector"` para tudo). Com `honor_labels: true`, o Prometheus preserva o `job` original de cada
métrica (`FinancialTransaction.Api` ou `FinancialTransaction.Worker`), essencial para os dashboards do Grafana
filtrarem por serviço.

### 2.4 Grafana: datasources e dashboards provisionados

[datasources.yaml](../infrastructure/docker/observability/grafana/provisioning/datasources/datasources.yaml)
registra Prometheus (`uid: prometheus`, datasource padrão) e Jaeger (`uid: jaeger`) automaticamente na subida do
container — nada precisa ser configurado manualmente na UI.

[dashboards.yaml](../infrastructure/docker/observability/grafana/provisioning/dashboards/dashboards.yaml) aponta
para os dashboards versionados em
[infrastructure/docker/observability/grafana/dashboards](../infrastructure/docker/observability/grafana/dashboards),
organizados em três arquivos:

| Dashboard | Painéis |
|---|---|
| `http-overview.json` — **HTTP (Api)** | HTTP requests por rota, HTTP duration (p50/p95/p99), HTTP errors (status ≥ 400), requisições em andamento. |
| `transaction-processing.json` — **Processamento de transações** | Transações criadas, transações processadas por status (Processed/Failed), duração do processamento da transação, duração do processamento de mensagem no Worker (ponta a ponta), erros de processamento e de publicação Kafka. |
| `kafka.json` — **Kafka** | Mensagens publicadas (Producer), mensagens consumidas (Consumer), publicado vs. consumido acumulado. |

Sobre **Kafka consumer lag** (citado como "se disponível" no objetivo da fase): não foi implementado. Medir lag
real exigiria um exportador adicional lendo os offsets de cada partição do Consumer Group direto do Kafka (ex.:
`kminion`, `kafka-lag-exporter` ou JMX + `jmx_exporter`) — nenhuma dessas peças existe na infraestrutura atual.
Como aproximação, o painel "Publicado vs. Consumido (acumulado)" do dashboard Kafka mostra a mesma informação de
forma indireta: se a linha de consumidas se afasta da linha de publicadas, há mensagens em atraso.

### 2.5 Métricas na Api e no Worker (`WithMetrics`)

Até a Fase 13, Api e Worker só tinham `WithTracing(...)` configurado — nenhuma métrica era emitida. Nesta fase,
ambos os `Program.cs` ganharam `WithMetrics(...)`, reaproveitando o mesmo `otlpEndpoint` (agora sem o sufixo de
sinal, já que cada exporter — traces e metrics — precisa de um path OTLP diferente: `/v1/traces` e `/v1/metrics`
respectivamente):

```csharp
.WithMetrics(metrics => metrics
    .AddMeter(ApplicationMetrics.MeterName)
    .AddMeter(InfrastructureMetrics.MeterName)   // só na Api
    .AddAspNetCoreInstrumentation()               // só na Api
    .AddHttpClientInstrumentation()               // só na Api
    .AddOtlpExporter(otlp =>
    {
        otlp.Endpoint = new Uri($"{otlpEndpoint}/v1/metrics");
        otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
    }));
```

- `AddAspNetCoreInstrumentation()` / `AddHttpClientInstrumentation()`, quando registradas dentro de
  `WithMetrics`, emitem métricas automáticas como `http.server.request.duration` (histograma) e
  `http.server.active_requests` — as mesmas bibliotecas de instrumentação usadas em `WithTracing`, mas gerando um
  sinal diferente (métrica em vez de span).
- `AddMeter(...)` registra, explicitamente, os `Meter`s manuais criados nesta fase (ver 2.6) — assim como
  `AddSource(...)` para traces, o SDK ignora qualquer `Meter` não registrado.
- O EF Core **não** tem instrumentação de métricas no pacote usado neste projeto (só traces), por isso não
  aparece em `WithMetrics`.

### 2.6 Métricas manuais (`Counter`/`Histogram`)

Seguindo o mesmo padrão dos `*Diagnostics` (ActivitySource) já existentes, cada camada que precisa emitir uma
métrica de negócio ganhou uma classe `*Metrics` com um `Meter` estático:

| Classe | Meter | Instrumentos |
|---|---|---|
| [ApplicationMetrics](../src/FinancialTransaction.Application/Common/Telemetry/ApplicationMetrics.cs) | `FinancialTransaction.Application` | `Counter` transações criadas; `Counter` transações processadas (tag `status`); `Histogram` duração do processamento (ms). |
| [InfrastructureMetrics](../src/FinancialTransaction.Infrastructure/Messaging/InfrastructureMetrics.cs) | `FinancialTransaction.Infrastructure` | `Counter` mensagens Kafka publicadas (tag `topic`); `Counter` erros de publicação (tag `topic`). |
| [WorkerMetrics](../src/FinancialTransaction.Worker/WorkerMetrics.cs) | `FinancialTransaction.Worker` | `Counter` mensagens Kafka consumidas (tag `topic`); `Counter` erros de processamento (tag `topic`); `Histogram` duração do processamento de mensagem, ponta a ponta (ms). |

Pontos de instrumentação:

- `TransactionService.CreateAsync` — incrementa `TransactionsCreated` após persistir e publicar o evento.
- `TransactionProcessingService.ProcessAsync` — mede a duração com `Stopwatch` e, ao final, registra
  `TransactionProcessingDuration` e incrementa `TransactionsProcessed` com a tag `status` (`Processed`/`Failed`).
- `KafkaEventPublisher.PublishAsync` — incrementa `KafkaMessagesPublished` no sucesso e `KafkaPublishErrors` na
  exceção `ProduceException`.
- `Worker.ProcessMessageAsync` — incrementa `KafkaMessagesConsumed` ao receber a mensagem, `ProcessingErrors` no
  `catch`, e registra `MessageProcessingDuration` num `finally` (cobre sucesso e falha).

Os nomes dos instrumentos seguem a convenção `financial_transaction.<área>.<coisa>` (pontos), que o exporter
`prometheus` converte para `financial_transaction_<área>_<coisa>` (underscores) — `Counter`s ganham o sufixo
`_total` e `Histogram`s os sufixos `_bucket`/`_sum`/`_count`, conforme o formato padrão do Prometheus.

## 3. Como investigar uma transação problemática

### 3.1 Subir toda a infraestrutura

```bash
docker compose up -d
docker compose ps   # confirmar postgres, kafka, kafka-ui, otel-collector, jaeger, prometheus, grafana "Up"
```

### 3.2 Rodar Api e Worker localmente

```bash
dotnet run --project src/FinancialTransaction.Api
dotnet run --project src/FinancialTransaction.Worker
```

### 3.3 Criar uma transação e guardar o TraceId

```bash
curl -i -X POST http://localhost:5209/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"sourceAccountId":"<id-conta-origem>","destinationAccountId":"<id-conta-destino>","amount":150.00}'
```

A resposta traz `X-Trace-Id: <traceId>` (ver Fase 10) — esse é o ponto de partida da investigação.

### 3.4 Localizar o TraceId no Jaeger

1. Abrir `http://localhost:16686`.
2. Em vez de buscar por serviço/operação, colar o TraceId diretamente na URL:
   `http://localhost:16686/trace/<traceId>`.
3. O Jaeger mostra a árvore completa de spans daquele trace — validado nesta implementação com todos os spans do
   fluxo:

```text
POST /api/transactions/                  (FinancialTransaction.Api, Server)
 └── TransactionService.CreateAsync      (FinancialTransaction.Application, Internal)
      ├── financialtransaction (SELECT/INSERT contas e transação — EF Core, Client)
      └── financial.transactions.created publish   (FinancialTransaction.Infrastructure, Producer)
           └── financial.transactions.created consume   (FinancialTransaction.Worker, Consumer)
                └── TransactionProcessingService.ProcessAsync   (FinancialTransaction.Application, Internal)
                     └── financialtransaction (SELECT/UPDATE — EF Core, Client)
```

Isso é exatamente o fluxo `Blazor → API → PostgreSQL → Kafka Producer → Kafka Consumer → Worker → PostgreSQL`
descrito no objetivo da fase — a única etapa que o Jaeger não mostra como span próprio é o `Blazor`, pois a Web
(Fase 5) ainda não foi instrumentada com OpenTelemetry (fora do escopo de todas as fases de observabilidade até
agora, que cobrem Api e Worker).

Para investigar uma transação **problemática**, os sinais a observar na árvore de spans:

- **Duração desproporcional** num span específico aponta o gargalo (ex.: um `Client` span de PostgreSQL muito mais
  longo que os demais).
- **Status de erro** (`Activity.SetStatus(ActivityStatusCode.Error, ...)`) aparece destacado em vermelho no Jaeger,
  com a mensagem de exceção nas tags — usado tanto no `KafkaEventPublisher` (falha de publicação) quanto no
  `Worker` (falha de processamento).
- **Span de consumo ausente ou desconectado** (TraceId diferente do span de publicação) indica falha na
  propagação de contexto pelo Kafka (ver [distributed-tracing.md](distributed-tracing.md), seção de
  troubleshooting).

### 3.5 Cruzar com as métricas no Grafana

1. Abrir `http://localhost:3000` (login `admin`/`admin`, ou acesso anônimo).
2. Pasta **FinancialTransaction** → um dos três dashboards (2.4).
3. Usar o intervalo de tempo do dashboard (canto superior direito) para restringir à janela em que a transação
   problemática ocorreu, e cruzar:
   - **HTTP (Api)** → duração/erros da chamada `POST /api/transactions/` naquele intervalo;
   - **Processamento de transações** → se `financial_transaction_transactions_processed_total{status="Failed"}`
     subiu, ou se a duração de processamento (`transaction_processing_duration`/`worker_message_processing_duration`)
     teve um pico;
   - **Kafka** → se a taxa de mensagens consumidas ficou atrás da taxa de publicadas (indício de lentidão ou
     acúmulo no Worker).

O TraceId permanece o fio condutor: o Jaeger explica **onde** e **por quê** uma transação específica falhou ou foi
lenta; o Grafana/Prometheus explicam se aquilo foi um caso isolado ou um padrão (taxa de erro/latência subindo
para todas as transações no mesmo período).

## 4. Fora do escopo desta fase (propositalmente)

- Instrumentação OpenTelemetry do `FinancialTransaction.Web` (Blazor) — o trace hoje começa no `POST` recebido
  pela Api, não no clique do usuário.
- Kafka consumer lag real (exigiria um exportador adicional — ver 2.4).
- Logs estruturados e sua correlação com TraceId/SpanId (Fase 15).
- Amostragem (`sampling`), retenção configurável ou alertas no Prometheus/Grafana.

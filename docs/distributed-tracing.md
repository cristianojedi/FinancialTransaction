# Distributed Tracing — Fase 12: propagação de contexto pelo Kafka

> Esta fase conecta o trace da `FinancialTransaction.Api` (Fase 10) ao trace do `FinancialTransaction.Worker`
> (Fase 11) através dos headers da mensagem Kafka. Collector, Prometheus, Grafana e Jaeger/Tempo continuam fora
> do escopo (Fases 13 e 14).

## 1. Conceitos

### 1.1 Distributed Tracing

Até a Fase 11, API e Worker geravam **traces independentes**: a API tinha um `TraceId` para `HTTP → Application →
PostgreSQL`, e o Worker tinha outro `TraceId`, sem relação nenhuma, para `Kafka Consumer → Worker → PostgreSQL`.
Distributed Tracing é a prática de fazer com que uma operação que atravessa múltiplos processos — aqui, API e
Worker, comunicando-se de forma assíncrona via Kafka — compartilhe **o mesmo `TraceId`**, permitindo reconstruir
a árvore de spans completa mesmo quando os processos não conversam diretamente entre si (não há uma chamada HTTP
síncrona entre API e Worker: a única ponte entre os dois é a mensagem Kafka).

### 1.2 W3C Trace Context

É uma especificação padronizada (W3C Recommendation) que define **como** o contexto de um trace deve ser
serializado e transportado entre processos, independentemente da linguagem, framework ou protocolo de transporte
usado. Antes dela, cada vendor de APM tinha seu próprio formato de propagação (proprietário), o que impedia
interoperabilidade entre ferramentas. Ela define dois headers: `traceparent` e `tracestate`.

### 1.3 `traceparent`

É o header (HTTP) — ou, por extensão, o campo transportado em qualquer meio de mensageria — que carrega o
contexto mínimo necessário para continuar um trace. Formato:

```text
traceparent: 00-<trace-id (32 hex)>-<parent-id (16 hex)>-<trace-flags (2 hex)>
```

Exemplo real capturado nesta implementação (ver seção 3): `00-7e7e05eaa53e1b812b006e772c5c7ae4-69de9e841754b3a8-01`.
Os quatro campos são: versão (`00`), `TraceId`, `SpanId` do span que está enviando a mensagem (vira o `ParentSpanId`
de quem recebe) e as flags (`01` = *sampled*, ou seja, este trace deve ser coletado).

### 1.4 TraceId

Identificador de 128 bits do trace inteiro (ver `docs/observability.md`, seção 1.6). Nesta fase, é o valor que
prova que API e Worker participaram da **mesma** operação distribuída.

### 1.5 SpanId

Identificador de 64 bits de um span específico (ver `docs/observability.md`, seção 1.7). O `SpanId` do span de
publicação da API (`Producer`) se torna o `ParentSpanId` do span de consumo do Worker (`Consumer`).

### 1.6 SpanContext

É o subconjunto mínimo e imutável de informação de um span necessário para propagá-lo para fora do processo:
`TraceId`, `SpanId` e `TraceFlags` (mais, opcionalmente, `TraceState`). No .NET, corresponde a
`System.Diagnostics.ActivityContext` — é exatamente esse objeto que é serializado no `traceparent` (Inject) e
reconstruído a partir dele (Extract).

### 1.7 Context Propagation

É o mecanismo geral de "levar" o `SpanContext` de um ponto do sistema a outro. Dentro de um único processo, isso
acontece automaticamente via `Activity.Current` (ambient context) — é assim que, na Fase 10, o span do EF Core
virava filho do span da Application sem nenhum código explícito. **Entre processos**, não existe esse mecanismo
automático: é preciso serializar o contexto na saída e desserializá-lo na entrada. HTTP resolve isso com um
header; Kafka não tem um conceito nativo de "header de protocolo" para isso — por isso a propagação via Kafka
precisa ser feita explicitamente pela aplicação, usando os *message headers* do Kafka como transporte.

### 1.8 Inject

É a operação de **serializar** o `SpanContext` atual (o `Activity.Current?.Context` no momento da publicação) no
formato W3C Trace Context e escrevê-lo em um transporte — neste projeto, nos headers da mensagem Kafka. É feito
pelo `Producer`, imediatamente antes de `ProduceAsync`.

### 1.9 Extract

É a operação inversa: **ler** o `traceparent` de um transporte (os headers da mensagem Kafka recebida) e
reconstruir um `ActivityContext`/`PropagationContext` a partir dele. É feito pelo `Consumer`, antes de iniciar o
span de consumo — o `ActivityContext` extraído é passado como **pai explícito** desse novo span.

### 1.10 Como o contexto é propagado através de HTTP

Em uma chamada HTTP instrumentada por `OpenTelemetry.Instrumentation.Http`/`AspNetCore`, o Inject/Extract já
acontece automaticamente: o cliente HTTP injeta o `traceparent` no header da requisição de saída, e o middleware
ASP.NET Core do lado receptor extrai esse header antes de criar o span `Server` daquela requisição — por isso, na
Fase 10, nunca foi preciso escrever código de propagação manualmente: a instrumentação automática do HTTP já
resolve isso "de graça".

### 1.11 Por que Kafka exige propagação explícita através dos headers

Kafka é um protocolo de mensageria assíncrona, não um protocolo de requisição/resposta como HTTP — não existe uma
"instrumentação automática de Kafka" universal e out-of-the-box no ecossistema .NET/Confluent.Kafka equivalente à
do ASP.NET Core, porque **o cliente Kafka usado (`Confluent.Kafka`) não tem hooks de `DiagnosticSource` que o
OpenTelemetry SDK possa escutar automaticamente**. Além disso, mesmo que existisse, o momento em que a mensagem é
publicada (span `Producer`) e o momento em que ela é consumida (span `Consumer`) podem estar **segundos ou minutos
separados no tempo**, em processos diferentes — não há uma conexão de rede ativa como em HTTP para "carregar"
contexto implicitamente. A única forma de conectar os dois momentos é a própria mensagem transportar essa
informação: por isso o `traceparent` precisa ser gravado explicitamente nos *headers* do `Message<TKey, TValue>`
do Kafka pelo código do Producer, e lido explicitamente dos headers do `ConsumeResult` pelo código do Consumer.

### 1.12 Producer Span x Consumer Span

- **Producer Span** (`ActivityKind.Producer`): representa o ato de publicar a mensagem. É filho do span que
  disparou a publicação (aqui, `TransactionService.CreateAsync`) e contém atributos como o tópico, a chave, a
  partition/offset resultantes.
- **Consumer Span** (`ActivityKind.Consumer`): representa o ato de receber/processar a mensagem. Não é filho do
  Producer Span *dentro do mesmo processo* — é filho dele **através do `traceparent` propagado**, já que os dois
  spans vivem em processos (e, tipicamente, momentos) diferentes.

Os dois `Kind`s existem justamente para deixar explícito, na visualização de um trace, onde a operação "atravessa"
um sistema de mensageria — diferente de `Client`/`Server` (usados em RPC síncrono como HTTP).

### 1.13 Relação parent/child entre spans

Nesta fase, a relação é **parent/child direta**: o Consumer Span tem, como `ParentSpanId`, o `SpanId` do Producer
Span (via o `ActivityContext` extraído do `traceparent`). Isso é o que a evidência real da seção 3 comprova.

### 1.14 Quando usar Span Links em arquiteturas orientadas a eventos

Parent/child assume uma relação 1:1 e síncrona-o-suficiente (um span "aguarda", conceitualmente, o outro). Isso
deixa de fazer sentido em cenários como:

- **Batch/fan-in**: um span processa *várias* mensagens de uma vez (ex.: um Worker que lê um lote e processa tudo
  em um único span) — não há um único "pai" natural, e sim vários candidatos.
- **Fan-out amplo**: uma mensagem é publicada uma vez e consumida por múltiplos Consumer Groups independentes,
  cada um em momentos muito distantes (horas depois) — modelar como filho direto tornaria o trace do Producer
  "aberto" por tempo indefinido, o que não reflete a realidade (o span do Producer já terminou há muito tempo).
- Nesses casos, usa-se **Span Links** (`ActivityLink`): uma referência a outro `SpanContext` **sem** estabelecer
  relação de parentesco — o span "sabe" que está relacionado a outro trace, mas cada um mantém seu próprio ciclo
  de vida independente. Este projeto não usa Span Links nesta fase porque o cenário é 1 Producer → 1 Consumer
  Group → processamento imediato, onde parent/child reflete fielmente a realidade; Links seriam a escolha certa
  se, por exemplo, um segundo Worker/Consumer Group processasse a mesma mensagem para fins de auditoria em um
  momento totalmente desacoplado.

## 2. O que foi implementado

```text
FinancialTransaction.Api
TraceId = ABC123
     │
     ├── HTTP Span            (automático, Fase 10)
     ├── Application Span     (manual, Fase 10)
     ├── PostgreSQL Span      (automático, Fase 10)
     └── Kafka Producer Span  (manual, NOVO — InfrastructureDiagnostics)
              │
              │ Inject: traceparent gravado nos headers da mensagem
              ▼
            Kafka
              │
              │ Extract: traceparent lido dos headers da mensagem
              ▼
FinancialTransaction.Worker
TraceId = ABC123 (o mesmo!)
     │
     ├── Kafka Consumer Span  (manual, Fase 11 — agora com pai explícito)
     ├── Process Transaction Span (manual, Fase 11)
     └── PostgreSQL Span      (automático, Fase 11)
```

### 2.1 Producer: criação do span e Inject

Arquivo: [KafkaEventPublisher.cs](../src/FinancialTransaction.Infrastructure/Messaging/KafkaEventPublisher.cs)

```csharp
using var activity = InfrastructureDiagnostics.ActivitySource.StartActivity(
    $"{topic} publish",
    ActivityKind.Producer);

activity?.SetTag("messaging.system", "kafka");
activity?.SetTag("messaging.destination", topic);
// ... demais tags ...

var message = new Message<string, string> { Key = key, Value = ..., Headers = new Headers { ... } };

var propagationContext = new PropagationContext(Activity.Current?.Context ?? default, Baggage.Current);
Propagators.DefaultTextMapPropagator.Inject(propagationContext, message.Headers, InjectHeader);

await _producer.ProduceAsync(topic, message, cancellationToken);
```

- O span `"{topic} publish"` (`ActivityKind.Producer`) é criado como filho do span ativo no momento da publicação
  — na prática, `TransactionService.CreateAsync` (Fase 10), então `HTTP → Application → Kafka Producer`.
- `Propagators.DefaultTextMapPropagator` é o propagador padrão do OpenTelemetry SDK (`OpenTelemetry.Api`),
  configurado por padrão como a combinação `TraceContextPropagator` (W3C `traceparent`/`tracestate`) +
  `BaggagePropagator`. Ele já sabe serializar um `PropagationContext` no formato correto — não é necessário
  montar a string `traceparent` manualmente.
- `InjectHeader` é o *setter* que ensina o propagador a escrever num `Confluent.Kafka.Headers`:
  `headers.Add(key, Encoding.UTF8.GetBytes(value))`.

### 2.2 Consumer: Extract e continuação do trace

Arquivo: [Worker.cs](../src/FinancialTransaction.Worker/Worker.cs)

```csharp
var propagationContext = Propagators.DefaultTextMapPropagator.Extract(
    default,
    consumeResult.Message.Headers,
    ExtractHeaderValues);

Baggage.Current = propagationContext.Baggage;

using var activity = WorkerDiagnostics.ActivitySource.StartActivity(
    $"{_options.TransactionsTopic} consume",
    ActivityKind.Consumer,
    propagationContext.ActivityContext);
```

- `ExtractHeaderValues` é o *getter* que ensina o propagador a ler do `Confluent.Kafka.Headers`:
  `headers.TryGetLastBytes(key, out var value)` → `Encoding.UTF8.GetString(value)`.
- A diferença central em relação à Fase 11 é o terceiro argumento de `StartActivity`: passar
  `propagationContext.ActivityContext` como **pai explícito** do novo span, em vez de deixar o SDK decidir
  sozinho (o que resultava sempre em uma nova raiz de trace).
- Se a mensagem não trouxer `traceparent` (por exemplo, uma mensagem publicada antes desta fase, ainda na fila),
  `propagationContext.ActivityContext` vem com o valor `default`, e o comportamento é idêntico ao da Fase 11: um
  trace novo é iniciado normalmente — a mudança é puramente aditiva e não quebra mensagens antigas.

### 2.3 Pacotes e registro da fonte de spans

- `OpenTelemetry.Api` 1.17.0 foi adicionado a `FinancialTransaction.Infrastructure.csproj` (que antes não tinha
  nenhuma dependência de OpenTelemetry) e a `FinancialTransaction.Worker.csproj`, para disponibilizar
  `Propagators`, `Baggage` e `PropagationContext`.
- Uma nova `ActivitySource` foi criada — [InfrastructureDiagnostics.cs](../src/FinancialTransaction.Infrastructure/Messaging/InfrastructureDiagnostics.cs),
  fonte `"FinancialTransaction.Infrastructure"` — e registrada em `AddSource(...)` no `Program.cs` da API (o
  Producer só roda na API; o Worker não precisa dela).

## 3. Como validar: TraceId compartilhado entre API e Worker

### 3.1 Subir a infraestrutura, a API e o Worker

```bash
docker compose up -d
dotnet run --project src/FinancialTransaction.Api
dotnet run --project src/FinancialTransaction.Worker
```

### 3.2 Criar uma transação

```bash
curl -i -X POST http://localhost:5209/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"sourceAccountId":"<id-conta-origem>","destinationAccountId":"<id-conta-destino>","amount":100.00}'
```

A resposta traz o `X-Trace-Id` da API (Fase 10), que é o mesmo `TraceId` a procurar no console do Worker.

### 3.3 Evidência real capturada nesta implementação

Console da **API** — span de publicação:

```text
Activity.TraceId:            7e7e05eaa53e1b812b006e772c5c7ae4
Activity.SpanId:             69de9e841754b3a8
Activity.DisplayName:        financial.transactions.created publish
Activity.Kind:               Producer
Activity.Tags:
    messaging.system: kafka
    messaging.destination: financial.transactions.created
    transaction.id: ab264f9b-95fe-413e-8401-feca61d810bc
    messaging.kafka.partition: 0
    messaging.kafka.offset: 10
Instrumentation scope (ActivitySource): FinancialTransaction.Infrastructure
```

Console do **Worker** — span de consumo, no mesmo trace:

```text
Activity.TraceId:            7e7e05eaa53e1b812b006e772c5c7ae4
Activity.SpanId:             cdad0e1a60a29b0d
Activity.ParentSpanId:       86bb458d2ae41f9d
Activity.DisplayName:        financial.transactions.created consume
Activity.Kind:               Consumer
Activity.Tags:
    messaging.system: kafka
    messaging.kafka.partition: 0
    messaging.kafka.offset: 10
    transaction.id: ab264f9b-95fe-413e-8401-feca61d810bc
Instrumentation scope (ActivitySource): FinancialTransaction.Worker
```

O `Activity.TraceId` é **idêntico** nos dois processos (`7e7e05eaa53e1b812b006e772c5c7ae4`), e o
`ParentSpanId` do span de consumo (`86bb458d2ae41f9d`) corresponde ao `SpanId` do span pai imediato dentro do
Worker (o span de consumo real, cujo pai é o span de publicação da API — a cadeia completa reconstrói
`HTTP → Application → Kafka Producer → Kafka Consumer → Process Transaction → PostgreSQL` como um único trace).

## 4. Como investigar quando a propagação do TraceId **não** funcionar

Se o `TraceId` do Worker vier **diferente** do `TraceId`/`X-Trace-Id` da API para a mesma transação, siga esta
ordem de investigação (de fora para dentro):

1. **O header `traceparent` chegou na mensagem?** Inspecione a mensagem no Kafka UI (`http://localhost:8080`) —
   os headers ficam visíveis por mensagem. Se `traceparent` não aparecer ali, o problema está do lado do
   Producer (passo 2). Se aparecer, o problema está do lado do Consumer (passo 3).
2. **O Producer não injetou o contexto.** Causas prováveis:
   - Não havia nenhuma `Activity` ativa no momento do `ProduceAsync` (`Activity.Current` era `null`) — isso
     acontece se o `ActivitySource` que deveria estar ativo (`ApplicationDiagnostics` ou
     `InfrastructureDiagnostics`) não estiver registrada via `AddSource(...)` no `Program.cs` da API, fazendo
     `StartActivity` retornar `null` silenciosamente (comportamento padrão do .NET quando não há listener).
   - O *setter* passado ao `Inject` está escrevendo no objeto `Headers` errado (por exemplo, um novo `Headers()`
     vazio em vez do que efetivamente vai para `producer.ProduceAsync`).
3. **O Consumer recebeu o header mas ainda assim criou um trace novo.** Causas prováveis:
   - `StartActivity` foi chamado sem passar o `parentContext` extraído (regressão para o comportamento da
     Fase 11) — confirme que a assinatura usada é
     `StartActivity(name, ActivityKind.Consumer, propagationContext.ActivityContext)` e não a versão de dois
     argumentos.
   - O *getter* passado ao `Extract` está lendo de um `Headers` diferente do `consumeResult.Message.Headers` real
     (por exemplo, por reaproveitar uma variável errada), ou retornando uma lista vazia por erro de decodificação
     (`Encoding.UTF8.GetString` sobre bytes corrompidos, ou chave com grafia diferente de `traceparent`).
   - `propagationContext.ActivityContext` veio com valor `default`: isso é **esperado e não é um bug** para
     mensagens publicadas antes desta fase (ainda na fila/tópico) — republique a mensagem ou crie uma transação
     nova para validar.
4. **Os dois `TraceId` batem, mas a hierarquia parece errada** (`ParentSpanId` não aponta para o `SpanId`
   esperado). Confirme qual span estava ativo (`Activity.Current`) no exato momento do `ProduceAsync` — se algum
   código intermediário criar e descartar uma `Activity` sem `using` correto, ou iniciar uma nova span sem
   finalizar a anterior, o Inject pode capturar um `SpanContext` diferente do que se espera visualmente na árvore.

## 5. Fora do escopo desta fase (propositalmente)

- OpenTelemetry Collector (Fase 13).
- Prometheus, Grafana, Jaeger/Tempo (Fase 14).
- Span Links (não há cenário de fan-out/batch neste projeto que os justifique — ver seção 1.14).

A propagação funciona de forma **aditiva e retrocompatível**: mensagens sem `traceparent` nos headers continuam
sendo processadas normalmente pelo Worker, apenas sem correlação com um trace de origem.

# FinancialTransaction — Fase 7: Kafka Producer

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 7 — Kafka Producer

## Objetivo

Alterar o fluxo:

```text
API
 │
 ├── PostgreSQL
 │
 └── Kafka
       │
       ▼
TransactionCreated
```

Topic:

```text
financial.transactions.created
```

### Definition of Done

- [x] Producer;
- [x] topic;
- [x] evento;
- [x] serialização;
- [x] publicação;
- [x] mensagem visualizada no Kafka UI.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Nesta fase implemente somente o Kafka Producer.

Evento:

TransactionCreated

Topic:

financial.transactions.created

O fluxo será:

POST /api/transactions
        |
        +--> PostgreSQL
        |
        +--> Kafka Producer
                 |
                 v
       financial.transactions.created

Explique:

1. O que é Producer.
2. O que é Topic.
3. Como Kafka serializa mensagens.
4. Como escolher a chave da mensagem.
5. O que acontece quando Kafka está indisponível.
6. Como lidar com falha de publicação.

Implemente:

- abstração de mensageria;
- producer Kafka;
- serialização JSON;
- configuração;
- publicação do evento.

Não implemente Consumer.
Não implemente Worker.
Não implemente OpenTelemetry.

Ao final:

1. Suba Kafka.
2. Crie ou valide o topic.
3. Execute POST.
4. Verifique a mensagem no Kafka UI.

Explique todo o fluxo.
```

---

## O que foi implementado

### Fluxo

```text
POST /api/transactions
        │
        ▼
TransactionService.CreateAsync
        │
        ├── Persiste FinancialTransaction (Pending) no PostgreSQL
        │
        └── Publica cada evento em transaction.DomainEvents
                 │
                 ▼
        IEventPublisher.PublishAsync(TransactionCreated)
                 │
                 ▼
        KafkaEventPublisher (Infrastructure)
                 │
                 ▼
        financial.transactions.created
```

A persistência no PostgreSQL acontece **antes** da publicação no Kafka: primeiro `_unitOfWork.SaveChangesAsync`, depois o `foreach` sobre `transaction.DomainEvents` publicando cada evento. Isso garante que só publicamos eventos de transações que já existem no banco.

### Abstração de mensageria

`src/FinancialTransaction.Application/Abstractions/Messaging/IEventPublisher.cs` — interface enxuta na camada `Application`, sem qualquer referência a Kafka:

```csharp
public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
```

Ela recebe o `IDomainEvent` genérico (já existente no `Domain` desde a Fase 2) — a `Application` não sabe (e não precisa saber) que a implementação por trás é Kafka. Isso segue o mesmo padrão já usado para persistência (`IUnitOfWork`, `IAccountRepository`, `IFinancialTransactionRepository`): abstração em `Application`, implementação concreta em `Infrastructure`.

### Producer Kafka

`src/FinancialTransaction.Infrastructure/Messaging/KafkaEventPublisher.cs` implementa `IEventPublisher` usando `Confluent.Kafka` (`IProducer<string, string>`):

- **Chave da mensagem**: `TransactionId` (como string). Todas as mensagens de uma mesma transação (neste momento só `TransactionCreated`, mas `TransactionProcessed`/`TransactionFailed` viriam depois) caem sempre na mesma partition, preservando a ordem de eventos por transação — importante para quando o Worker (Fase 8) consumir e precisar processar os eventos de uma transação em ordem.
- **Valor da mensagem**: o evento serializado em JSON via `System.Text.Json`.
- **Headers**: `event-type` com o nome da classe do evento (`TransactionCreated`), permitindo a um consumer decidir como desserializar sem precisar inspecionar o payload primeiro.
- **Resolução de topic**: um `switch` expression mapeia o tipo do evento para o topic configurado (`TransactionCreated` → `Kafka:TransactionsTopic`). Lança `InvalidOperationException` para qualquer evento sem topic mapeado — hoje só existe um evento publicado, então é intencionalmente simples; novos eventos exigirão um novo `case` e uma nova entrada de configuração.
- **Configuração do producer**: `Acks = Acks.All` (só considera a mensagem publicada quando todas as réplicas em sync confirmarem — no cluster local de 1 broker, equivale ao próprio broker), `EnableIdempotence = true` (evita duplicação de mensagens em caso de retry interno do client) e `MessageTimeoutMs = 5000`.

O producer (`IProducer<string, string>`) e o `KafkaEventPublisher` são registrados como `Singleton` em `src/FinancialTransaction.Infrastructure/DependencyInjection.cs` — o client do Confluent.Kafka é thread-safe e caro de criar (abre conexões com os brokers), então deve viver por toda a aplicação, ao contrário dos repositórios (`Scoped`, atrelados ao `DbContext` por requisição).

### Configuração

`src/FinancialTransaction.Infrastructure/Messaging/KafkaOptions.cs` + seção `Kafka` em `appsettings.json`:

```json
"Kafka": {
  "BootstrapServers": "localhost:9092",
  "TransactionsTopic": "financial.transactions.created"
}
```

`localhost:9092` é o listener externo do Kafka (`PLAINTEXT_HOST`) configurado na Fase 6, alcançável pela Api rodando localmente via `dotnet run`. Quando a Api for dockerizada (Fase 19), esse valor passa a ser `kafka:19092` (listener interno) via `appsettings.*` de ambiente ou variável de ambiente.

### Serialização

Kafka não serializa nada por conta própria — ele trata chave e valor como sequências de bytes opacas; quem decide o formato é o producer/consumer através dos *serializers*. Aqui usamos os serializers padrão de `string` do Confluent.Kafka (`Serializers.Utf8`, implícitos em `IProducer<string, string>`) para a chave, e serializamos o evento manualmente para uma `string` JSON com `System.Text.Json.JsonSerializer.Serialize` antes de atribuí-lo a `Message.Value`. Isso mantém o payload legível no Kafka UI e evita acoplar o projeto a um schema registry (Avro/Protobuf) nesta fase — algo que poderia ser considerado em uma fase futura de amadurecimento, mas está fora do escopo didático atual.

### Escolha da chave da mensagem

A chave (`Message.Key`) determina em qual partition a mensagem cai (via hash da chave, quando não se especifica a partition explicitamente). Usar o `TransactionId` garante que:

1. Mensagens da mesma transação sempre vão para a mesma partition, preservando ordem entre elas.
2. Transações diferentes tendem a se distribuir entre partitions diferentes, permitindo paralelismo entre múltiplos consumers de um mesmo Consumer Group (Fase 8).

Uma alternativa seria usar a conta de origem como chave (garantindo ordem por conta), mas isso não é necessário para o caso de uso atual e concentraria mensagens de uma conta muito ativa em uma única partition.

### O que acontece quando o Kafka está indisponível

O `IProducer<string,string>` é criado de forma "lazy": `ProducerBuilder.Build()` não abre conexão imediatamente, só na primeira tentativa de `ProduceAsync`. Se o Kafka estiver fora do ar (ou inalcançável) no momento do POST:

1. O `ProduceAsync` fica tentando entregar a mensagem até `MessageTimeoutMs` (5 segundos configurados aqui) esgotar.
2. Esgotado o timeout, o client lança `ProduceException<string, string>`.
3. `KafkaEventPublisher.PublishAsync` loga o erro (`_logger.LogError`, com o motivo reportado pelo broker) e relança a exceção.
4. A exceção sobe até o `GlobalExceptionHandler`, que não tem um mapeamento específico para `ProduceException` e cai no caso `_ => 500`, retornando um `ProblemDetails` genérico ao cliente HTTP.

**Importante**: nesse cenário, a transação **já foi persistida como `Pending` no PostgreSQL** antes da tentativa de publicação (`SaveChangesAsync` acontece primeiro). Ou seja, o registro existe no banco, mas o evento nunca chegou ao Kafka — o Worker (Fase 8) jamais vai processá-lo, e a transação fica presa em `Pending` indefinidamente. O cliente HTTP recebe um erro 500, então sabe que algo falhou, mas não tem como saber, só pelo response, que o registro no PostgreSQL foi criado.

Essa inconsistência (persistido no banco, não publicado no Kafka) é uma limitação conhecida e aceita nesta fase — resolvê-la de forma robusta (outbox pattern: gravar o evento na mesma transação do PostgreSQL e ter um processo separado publicando-o no Kafka com garantia de entrega) está fora do escopo. As Fases 16 (Resiliência) e 17 (Idempotência) do roadmap tratam desse tipo de problema de forma mais estruturada.

### Como lidar com falha de publicação

Nesta fase, a estratégia é deliberadamente simples: **falhar de forma visível** (propagar a exceção, retornar 500) em vez de falhar silenciosamente (logar e engolir o erro). Engolir o erro faria a API responder `201 Created` para uma transação que nunca será processada, o que é pior — o cliente acredita que tudo correu bem quando não correu.

O que **não** foi implementado nesta fase (fica para fases futuras do roadmap):

- Retry com backoff (Fase 16).
- Circuit breaker (Fase 16).
- Outbox pattern / garantia transacional entre PostgreSQL e Kafka.
- Dead Letter Topic (Fase 18) — não se aplica ao producer, é um conceito do lado do consumer/Worker.

### Testes

- **Unitários** (`tests/FinancialTransaction.UnitTests/Application/TransactionServiceTests.cs`): um `NoOpEventPublisher` fake (mesmo padrão do `NoOpUnitOfWork` já existente) captura os eventos publicados em uma lista, sem tocar em Kafka. Um novo teste (`CreateAsync_com_contas_validas_publica_evento_TransactionCreated`) garante que `TransactionService.CreateAsync` publica exatamente um `TransactionCreated` com os dados corretos.
- **Integração** (`tests/FinancialTransaction.IntegrationTests/Api/TransactionsApiFixture.cs`): a fixture agora sobe também um `KafkaContainer` real via Testcontainers (`Testcontainers.Kafka`), além do `PostgreSqlContainer` já existente, e configura `Kafka:BootstrapServers`/`Kafka:TransactionsTopic` no `WebApplicationFactory`. Assim, `POST /api/transactions` nos testes de integração publica de fato no tópico de um Kafka descartável, validando o fluxo completo (incluindo serialização e resolução de topic) sem depender do Kafka de desenvolvimento (`docker-compose.yml`).

### Validação executada

Com a infraestrutura da Fase 6 já no ar (`docker compose up -d` — `postgres`, `kafka`, `kafka-ui` saudáveis):

```bash
dotnet build FinancialTransaction.slnx        # 0 erros
dotnet test tests/FinancialTransaction.UnitTests         # 25 aprovados
dotnet test tests/FinancialTransaction.IntegrationTests  # 10 aprovados (inclui Kafka via Testcontainers)
```

Em seguida, a Api foi executada localmente (`dotnet run`, `http://localhost:5080`) e testada manualmente:

```bash
curl -s http://localhost:5080/api/accounts
# [{"id":"dc308aaa-...","number":"ACC-001"},{"id":"fe7ddc1c-...","number":"ACC-002"}]

curl -s -X POST http://localhost:5080/api/transactions \
  -H "Content-Type: application/json" \
  -d '{"sourceAccountId":"dc308aaa-...","destinationAccountId":"fe7ddc1c-...","amount":1500.00}'
# HTTP 201 — {"id":"64c87667-...","status":"Pending",...}
```

Log da Api confirmando a publicação:

```text
info: FinancialTransaction.Infrastructure.Messaging.KafkaEventPublisher[0]
      Evento TransactionCreated publicado no topic financial.transactions.created (partition 0, offset 0).
```

Tópico criado automaticamente no primeiro `ProduceAsync` (auto-creation padrão do broker) e confirmado via CLI dentro do container:

```bash
docker exec financialtransaction_kafka /opt/kafka/bin/kafka-topics.sh --bootstrap-server localhost:9092 --list
# financial.transactions.created
```

Mensagem lida diretamente do tópico, confirmando chave e payload:

```bash
docker exec financialtransaction_kafka /opt/kafka/bin/kafka-console-consumer.sh \
  --bootstrap-server localhost:9092 --topic financial.transactions.created \
  --from-beginning --property print.key=true --property key.separator=" | "
# 64c87667-... | {"TransactionId":"64c87667-...","SourceAccountId":"dc308aaa-...","DestinationAccountId":"fe7ddc1c-...","Amount":1500.00,"OccurredOnUtc":"2026-08-02T18:35:53.2546316Z"}
```

E validado via API REST do Kafka UI (`http://localhost:8080`, cluster `financialtransaction`):

```bash
curl -s http://localhost:8080/api/clusters/financialtransaction/topics/financial.transactions.created
# partitionCount: 1, offsetMax: 1 — uma mensagem publicada
```

Confirma o DoD: a mensagem publicada pela Api aparece no tópico `financial.transactions.created`, visível tanto via CLI quanto via Kafka UI.

### Fora do escopo desta fase (propositalmente)

- Nenhum Consumer ou Worker foi implementado — o evento fica no tópico aguardando a Fase 8.
- Nenhuma instrumentação OpenTelemetry foi adicionada (Fases 10–13).
- Retry, circuit breaker, outbox pattern e idempotência ficam para as Fases 16–17.

---
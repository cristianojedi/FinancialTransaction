# FinancialTransaction — Fase 8: Kafka Consumer + Worker

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 8 — Kafka Consumer + Worker

## Objetivo

Criar:

```text
FinancialTransaction.Worker
```

Fluxo:

```text
Kafka
 │
 ▼
Consumer
 │
 ▼
Worker
 │
 ▼
Application
 │
 ▼
PostgreSQL
```

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Nesta fase implemente o FinancialTransaction.Worker.

O Worker deve consumir:

financial.transactions.created

Explique:

1. O que é BackgroundService.
2. O que é Consumer Group.
3. Como Kafka controla Offset.
4. O que acontece quando o Worker reinicia.
5. Como tratar exceções.
6. Como evitar perder mensagens.

O Worker deve:

1. Consumir mensagem.
2. Desserializar TransactionCreated.
3. Localizar transação.
4. Alterar status para Processing.
5. Executar processamento.
6. Alterar para Processed ou Failed.

Não implemente ainda:

- OpenTelemetry.
- DLQ.
- Idempotência avançada.
- Retry avançado.

Teste:

Blazor
 -> API
 -> PostgreSQL
 -> Kafka
 -> Worker
 -> PostgreSQL

Ao final explique como validar cada etapa.
```

---

## Conceitos

### 1. O que é `BackgroundService`

`BackgroundService` é uma classe base do `Microsoft.Extensions.Hosting` para implementar serviços de longa duração que rodam em segundo plano dentro de um `IHost`. Basta sobrescrever `ExecuteAsync(CancellationToken)`; o host chama esse método quando a aplicação inicia e sinaliza o `CancellationToken` quando pede o encerramento (Ctrl+C, `docker stop`, etc.). É o mesmo mecanismo usado por `FinancialTransaction.Worker`, mas aqui o corpo do método é um laço `while` que consome mensagens do Kafka continuamente em vez de terminar após uma única execução.

### 2. O que é Consumer Group

Um Consumer Group é um conjunto de consumers identificados por um `GroupId` (aqui, `financial-transaction-worker`, configurável em `Kafka:ConsumerGroupId`) que dividem entre si as partições de um topic. O Kafka garante que cada partição seja lida por **no máximo um** consumer do grupo por vez — isso permite escalar horizontalmente (rodar múltiplas instâncias do Worker) sem processar a mesma mensagem duas vezes na mesma partição, e permite que o grupo retome de onde parou after um restart, pois o offset consumido é gravado por `GroupId + Topic + Partition`.

### 3. Como o Kafka controla Offset

Cada mensagem em uma partição tem um número sequencial crescente, o offset. O broker mantém, por Consumer Group, qual foi o último offset confirmado (commitado) para cada partição. Neste projeto o consumer é configurado com `EnableAutoCommit = false`: o commit é feito manualmente (`_consumer.Commit(consumeResult)`) **somente depois** que `ITransactionProcessingService.ProcessAsync` termina com sucesso. Se o processamento falhar, o offset não avança, e a mensagem será entregue novamente na próxima leitura daquela partição.

### 4. O que acontece quando o Worker reinicia

Como o offset só é commitado após o sucesso do processamento, ao reiniciar o Worker retoma a leitura a partir do último offset commitado (`AutoOffsetReset = Earliest` é usado apenas na primeira execução de um Consumer Group novo, sem offset prévio). Isso implica em **at-least-once delivery**: uma mensagem cujo processamento foi interrompido no meio (ex.: o processo caiu depois de gravar `Processing` mas antes de commitar) será reentregue. `TransactionProcessingService.ProcessAsync` lida com esse cenário: se a transação já está em `Processing` (não em `Pending`), ele não chama `StartProcessing()` de novo (o que lançaria `DomainException`) — apenas retoma a validação das contas e conclui o processamento. Se a transação já está em um estado final (`Processed`/`Failed`), a mensagem é apenas logada e ignorada, evitando reprocessamento indevido.

### 5. Como tratar exceções

O consumo roda dentro de um `try/catch` por mensagem (`ProcessMessageAsync`). Exceções de desserialização, de negócio (`NotFoundException` quando a transação não existe) ou de infraestrutura (falha de banco) são capturadas e logadas com `LogError`, incluindo partition/offset para facilitar a correlação no Kafka UI. O laço principal continua rodando para a próxima mensagem; falhas ao consumir do broker (`ConsumeException`) também são logadas sem derrubar o Worker.

### 6. Como evitar perder mensagens

A combinação de `EnableAutoCommit = false` + commit manual só após sucesso é a garantia central: uma mensagem nunca é "esquecida" antes de ser processada com sucesso. O trade-off — deliberadamente aceito nesta fase — é que uma mensagem permanentemente inválida (ex.: `TransactionId` que nunca existirá) ficará sendo reentregue indefinidamente a cada restart, já que ainda não há DLQ nem limite de tentativas (fases 16 a 18). `Acks = Acks.All` e `EnableIdempotence = true` do lado do Producer (Fase 7) complementam essa garantia, evitando perda ou duplicação na publicação.

## O que foi implementado

- **`FinancialTransaction.Worker` (`Worker.cs`)** — passou de stub vazio para um `BackgroundService` que injeta `IConsumer<string, string>`, `IServiceScopeFactory`, `IOptions<KafkaOptions>` e `ILogger<Worker>`. `ExecuteAsync` assina o topic `financial.transactions.created` e consome em loop até o `stoppingToken` ser cancelado; cada mensagem é desserializada diretamente para `FinancialTransaction.Domain.Events.TransactionCreated` (o mesmo tipo publicado pelo Producer da Fase 7 — sem DTO duplicado) e processada em um `IServiceScope` próprio (o `TransactionProcessingService` é `Scoped`, pois depende do `DbContext`).
- **`ITransactionProcessingService` / `TransactionProcessingService`** (novo, em `FinancialTransaction.Application/Transactions/`) — orquestra o caso de uso "processar transação": busca a transação (`NotFoundException` se não existir), ignora se já está em estado final, marca `Processing` (persistindo antes de seguir), valida a existência das contas origem/destino e conclui como `Processed` ou `Failed`, sempre persistindo via `IUnitOfWork.SaveChangesAsync`. Segue o mesmo padrão de serviço (sem MediatR) já usado por `TransactionService`. Registrado como `Scoped` em `Application/DependencyInjection.cs`.
- **`KafkaOptions`** — ganhou a propriedade `ConsumerGroupId` (seção `Kafka:ConsumerGroupId`).
- **`Infrastructure/DependencyInjection.cs`** — passou a registrar também `IConsumer<string, string>` como singleton, com `AutoOffsetReset.Earliest` e `EnableAutoCommit = false`. É a mesma `AddInfrastructure` usada pela API (que só resolve o Producer) e pelo Worker (que resolve Producer, via `IEventPublisher`, e Consumer); como o registro é uma factory lazy, a API nunca chega a construir um Consumer.
- **`Program.cs` do Worker** — agora chama `AddApplication()` e `AddInfrastructure(builder.Configuration)` antes de registrar o `Worker` como `HostedService`.
- **`appsettings.json` do Worker** — ganhou `ConnectionStrings:PostgreSql` e a seção `Kafka` (`BootstrapServers`, `TransactionsTopic`, `ConsumerGroupId`), espelhando a API.
- **Testes unitários** (`TransactionProcessingServiceTests`) — cobrem: processamento com sucesso (`Processed`), conta inexistente (`Failed` com motivo), transação inexistente (`NotFoundException`), transação já em estado final (idempotência simples) e retomada de uma transação presa em `Processing` (simulando reinício do Worker).

Não implementado nesta fase (propositalmente, conforme escopo): OpenTelemetry, Dead Letter Topic, idempotência avançada (deduplicação por `EventId`) e retry avançado (backoff/circuit breaker) — ficam para as fases 11, 16, 17 e 18.

## Como validar

1. Suba a infraestrutura (se ainda não estiver no ar):
   ```bash
   docker compose up -d
   docker compose ps
   ```
2. Rode a API e o Worker em terminais separados:
   ```bash
   dotnet run --project src/FinancialTransaction.Api
   dotnet run --project src/FinancialTransaction.Worker
   ```
   O log do Worker deve mostrar `Worker inscrito no tópico financial.transactions.created (consumer group financial-transaction-worker).`.
3. (Opcional) Rode o Blazor: `dotnet run --project src/FinancialTransaction.Web`.
4. Crie uma transação (via Blazor ou `POST /api/transactions`) e confira a resposta imediata com `status: "Pending"`.
5. No Kafka UI (`http://localhost:8080`), confira a mensagem publicada no topic `financial.transactions.created` e, na aba de Consumer Groups, o grupo `financial-transaction-worker` avançando o offset.
6. Consulte `GET /api/transactions/{id}` alguns instantes depois: o status deve ter mudado para `Processed` (contas válidas) ou `Failed` (conta inexistente). O log do Worker mostra as queries EF Core (`Processing` → `Processed`/`Failed`) e a linha `Transação {id} processada (partition …, offset …)`.
7. Para observar a resiliência a restart: pare o Worker (`Ctrl+C`) logo depois de criar uma transação, confirme que ela fica `Pending`/`Processing`, e suba o Worker novamente — ele deve retomar e concluir o processamento (`docker compose stop`/`start` no Worker, se estiver containerizado, tem o mesmo efeito).
8. Rode os testes automatizados:
   ```bash
   dotnet test tests/FinancialTransaction.UnitTests
   ```

Validado manualmente nesta fase: build (`dotnet build FinancialTransaction.slnx`), `dotnet test` dos testes unitários (30 aprovados) e o fluxo real ponta a ponta — `POST /api/transactions` → `Pending` → Worker consumiu do Kafka → `GET /api/transactions/{id}` retornou `Processed`.

---

## Extra — Grid de transações e exclusão no Blazor

Fora do escopo original desta fase, foi adicionada uma evolução no frontend para dar visibilidade ao fluxo `Pending → Processing → Processed/Failed` processado pelo Worker: uma grid paginada de transações na tela inicial, com um campo de data de cadastro e ação de exclusão.

### O que foi implementado

- **Domain (`FinancialTransaction.cs`)** — novo campo `CreatedAtUtc` (`DateTime`, `private set`), atribuído uma única vez no construtor privado no momento da criação (`Create`). Segue o mesmo padrão imutável de `Status`.
- **Infrastructure**:
  - `FinancialTransactionConfiguration` — mapeamento `IsRequired()` para `CreatedAtUtc` e um índice (`HasIndex`) para suportar a ordenação da grid.
  - Migration `20260802225450_AddCreatedAtUtcToTransactions` — adiciona a coluna `CreatedAtUtc` (`timestamp with time zone`, `NOT NULL`) e o índice `IX_transactions_CreatedAtUtc` na tabela `transactions`.
  - `FinancialTransactionRepository` — novo método `DeleteAsync(transaction, ct)`, que apenas marca a entidade para remoção (`_dbContext.Transactions.Remove(...)`); o `SaveChanges` continua centralizado no `IUnitOfWork`, chamado pela camada de aplicação.
- **Application**:
  - `IFinancialTransactionRepository.DeleteAsync` e `ITransactionService.DeleteAsync` — novo caso de uso "excluir transação": busca a transação (`NotFoundException` se não existir), remove via repositório e persiste com `IUnitOfWork.SaveChangesAsync`.
  - `TransactionResponse` — ganhou o campo `CreatedAtUtc`, mapeado em `FromDomain`.
- **Api (`TransactionEndpoints.cs`)** — novo endpoint `DELETE /api/transactions/{id}`, retornando `204 No Content` em caso de sucesso e `404` (via `GlobalExceptionHandler` + `NotFoundException`) quando a transação não existe.
- **Web (Blazor + MudBlazor)**:
  - `IFinancialApiClient`/`FinancialApiClient` — novos métodos `GetTransactionsAsync()` (`GET /api/transactions`) e `DeleteTransactionAsync(id)` (`DELETE /api/transactions/{id}`), seguindo o mesmo padrão de tratamento de erro (`ApiException`) já usado pelos demais métodos.
  - `Home.razor` — abaixo do formulário de criação, uma `MudDataGrid<TransactionResponse>` paginada (paginação nativa do componente), ordenada por `CreatedAtUtc` de forma decrescente (mais recentes primeiro), com colunas de data de cadastro, conta origem, conta destino, valor (formatado em `C2`) e status. A última coluna traz um `MudIconButton` de exclusão que abre um diálogo de confirmação (`IDialogService.ShowMessageBoxAsync`) antes de chamar a API; a grid é recarregada automaticamente após criar ou excluir uma transação, com feedback via `Snackbar`.
- **Testes unitários** — novos casos para `TransactionService.DeleteAsync` (exclusão com sucesso e transação inexistente lançando `NotFoundException`), usando os fakes em memória já existentes.

### Como validar

1. Suba a infraestrutura, a API e o Blazor:
   ```bash
   docker compose up -d
   dotnet run --project src/FinancialTransaction.Api
   dotnet run --project src/FinancialTransaction.Web
   ```
2. Acesse a tela inicial do Blazor: abaixo do formulário de criação, a grid "Transações" lista os registros existentes, ordenados pela data de cadastro mais recente.
3. Crie uma nova transação pelo formulário — ela deve aparecer automaticamente no topo da grid, com a data de cadastro preenchida.
4. Clique no ícone de exclusão de uma linha, confirme no diálogo — a transação deve sumir da grid e uma mensagem de sucesso deve aparecer via Snackbar.
5. Tente excluir a mesma transação diretamente na API (`DELETE /api/transactions/{id}` de um Id já excluído) e confirme o retorno `404`.
6. Rode os testes automatizados: `dotnet test tests/FinancialTransaction.UnitTests` (32 testes aprovados após esta etapa).

Validado manualmente: build completo da solução, `dotnet test` (32 aprovados) e teste end-to-end via navegador (Playwright) — criação de transação refletida na grid com data de cadastro correta, exclusão com confirmação funcionando e grid atualizada em tempo real.

---
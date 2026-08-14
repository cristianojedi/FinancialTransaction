# FinancialTransaction — Fase 9: Fluxo financeiro completo

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 9 — Fluxo financeiro completo

## Objetivo

Conectar tudo.

```text
Blazor
  │
  ▼
API
  │
  ├── PostgreSQL
  │
  └── Kafka
       │
       ▼
     Worker
       │
       ▼
   PostgreSQL
```

### Prompt para IA

```text
Agora integre todas as partes já implementadas.

Fluxo:

Blazor
 -> API
 -> PostgreSQL
 -> Kafka
 -> Worker
 -> PostgreSQL

O frontend deve permitir:

1. Criar transação.
2. Receber ID.
3. Consultar status.
4. Atualizar status visualmente.

Implemente polling simples para consultar o status.

Explique:

1. Processamento síncrono.
2. Processamento assíncrono.
3. Eventual consistency.
4. Por que a resposta inicial pode ser Pending.
5. Por que o resultado final não está disponível imediatamente.

Não implemente OpenTelemetry ainda.

Crie um teste end-to-end do fluxo completo.
```

---

## Implementação realizada

Toda a infraestrutura (API, Worker, Kafka, Blazor) já existia das fases 4 a 8. O trabalho
desta fase foi conectar as pontas soltas: consulta de status pelo frontend e um teste que
valida o fluxo real de ponta a ponta.

### Frontend (Blazor)

- [`IFinancialApiClient`](../src/FinancialTransaction.Web/Services/IFinancialApiClient.cs) e
  [`FinancialApiClient`](../src/FinancialTransaction.Web/Services/FinancialApiClient.cs)
  ganharam `GetTransactionByIdAsync`, usado para consultar `GET /api/transactions/{id}`.
- [`Home.razor`](../src/FinancialTransaction.Web/Components/Pages/Home.razor) passou a fazer
  **polling simples** após criar a transação: a cada 2s, por até 30s, consulta o status até
  ele chegar a um estado final (`Processed` ou `Failed`).
- O status é exibido visualmente com `MudChip` colorido (Pending = cinza, Processing = azul,
  Processed = verde, Failed = vermelho), tanto no card de resultado quanto na grid de
  transações, que também é atualizada a cada ciclo de polling.
- Um indicador "Consultando status..." aparece enquanto o polling está ativo.
- O componente implementa `IDisposable` para cancelar o polling se a página for fechada.

### Teste end-to-end

Criado em `tests/FinancialTransaction.IntegrationTests/EndToEnd/`:

- **`FullFlowFixture.cs`** — sobe containers reais de PostgreSQL e Kafka via Testcontainers,
  hospeda a API (`WebApplicationFactory<Program>`) e o `Worker` (mesmo `BackgroundService`
  usado em produção, rodando em um `IHost` dentro do próprio processo de teste), todos
  apontando para os mesmos containers.
- **`FullFlowTests.cs`** — dois testes:
  1. `Fluxo_completo_cria_transacao_pending_e_worker_a_processa_ate_Processed`: cria contas,
     faz `POST /api/transactions` (simulando a chamada do Blazor), confirma `201 Created`
     com status `Pending`, faz polling em `GET /api/transactions/{id}` (mesma técnica usada
     no Blazor) até status final, e confirma `Processed` tanto na resposta da API quanto
     direto no PostgreSQL.
  2. `Fluxo_completo_com_conta_excluida_apos_publicacao_worker_marca_como_Failed`: remove a
     conta de destino depois do evento publicado, forçando o Worker a marcar a transação
     como `Failed`, validando o caminho de erro do mesmo fluxo.

### Resultado da validação

```text
dotnet test FinancialTransaction.slnx
FinancialTransaction.UnitTests:        32 aprovados
FinancialTransaction.IntegrationTests: 12 aprovados (inclui os 2 testes E2E)
```

OpenTelemetry não foi implementado, conforme instruído.

---

## Como rodar o teste E2E

O teste sobe containers reais (PostgreSQL + Kafka) via Testcontainers, então **o Docker
precisa estar em execução** antes de rodar.

### Via linha de comando

```bash
dotnet test tests/FinancialTransaction.IntegrationTests --filter "FullyQualifiedName~EndToEnd"
```

### Via Visual Studio

1. Abra `FinancialTransaction.slnx`.
2. Garanta que o Docker Desktop está rodando.
3. Menu **Testar → Explorador de Testes** (`Ctrl+E, T`).
4. Se a árvore não aparecer, compile a solução primeiro (`Ctrl+Shift+B`) — o Explorador de
   Testes só lista testes de assemblies já compilados.
5. Na árvore, localize `FinancialTransaction.IntegrationTests` →
   `FinancialTransaction.IntegrationTests.EndToEnd.FullFlowTests`.
6. Clique com o botão direito nos testes (ou na classe inteira) e escolha **Executar** —
   ou **Depurar**, se quiser colocar breakpoints, inclusive dentro do `Worker`, já que ele
   roda no mesmo processo de teste.

**Observações:**

- Na primeira execução, o Testcontainers baixa as imagens `postgres:16-alpine` e
  `confluentinc/cp-kafka:7.7.1` — pode levar alguns minutos. Execuções seguintes usam o
  cache local e levam poucos segundos (~3-6s).
- Os dois testes de `FullFlowTests` compartilham a mesma fixture (`IClassFixture`), então
  os containers sobem uma única vez para a classe inteira.

---

## Processamento síncrono vs. assíncrono

**Síncrono** é a parte do fluxo que acontece dentro da própria requisição HTTP do Blazor
para a API, e que o cliente espera terminar antes de receber uma resposta:

```text
Blazor --POST--> API --INSERT--> PostgreSQL --produce--> Kafka --> resposta HTTP
```

A API só devolve a resposta depois que a transação foi persistida como `Pending` e o
evento `TransactionCreated` foi publicado no Kafka. Tudo isso é síncrono do ponto de vista
do cliente HTTP: ele fica bloqueado até a API responder.

**Assíncrono** é o processamento que acontece *depois* da resposta HTTP, desacoplado do
tempo de vida da requisição:

```text
Kafka --consume--> Worker --processa--> PostgreSQL (UPDATE)
```

O Worker roda como um `BackgroundService` independente, lendo o tópico
`financial.transactions.created` no seu próprio ritmo. Ele pode processar a mensagem um
milissegundo depois ou vários segundos depois — o Blazor já recebeu sua resposta HTTP há
muito tempo e não tem mais nenhuma conexão aberta esperando por isso.

## Eventual consistency (consistência eventual)

O sistema tem dois "observadores" do mesmo dado (a transação) que podem enxergar estados
diferentes ao mesmo tempo:

- O PostgreSQL, logo após o POST, tem a transação como `Pending`.
- Alguns milissegundos (ou segundos) depois, o Worker atualiza a mesma linha para
  `Processing` e depois para `Processed`/`Failed`.

Entre esses dois momentos existe uma janela em que o estado "verdadeiro" (o que a UI
mostra) ainda não é o estado "final" (o que ele vai se tornar). Isso é consistência
eventual: **não há garantia de que uma leitura logo após a escrita reflita o resultado
final do processamento** — apenas a garantia de que, dado tempo suficiente e nenhuma
falha permanente, o sistema converge para o estado correto.

Isso é diferente de uma transação de banco de dados tradicional (ACID, consistência
imediata), onde ler logo após escrever sempre reflete o valor mais recente e definitivo.
Aqui, a etapa "definitiva" nem sequer aconteceu ainda quando o POST retorna.

## Por que a resposta inicial pode ser Pending

A API deliberadamente **não** espera o Worker processar a transação antes de responder.
Ela faz apenas duas coisas de forma síncrona: gravar a transação como `Pending` e publicar
o evento no Kafka. O motivo é desacoplamento:

- Se a API esperasse o processamento completo (validação de conta, saldo etc.), o tempo de
  resposta do POST dependeria da disponibilidade e da carga do Worker — algo que o cliente
  HTTP não deveria precisar saber.
- Publicar-e-responder mantém a API rápida e resiliente: mesmo que o Worker esteja lento,
  parado ou reiniciando, o POST continua funcionando e a transação fica registrada como
  `Pending`, aguardando para ser processada assim que o Worker voltar.

Por isso `Pending` é o único status possível na resposta do `POST /api/transactions`.

## Por que o resultado final não está disponível imediatamente

O resultado final depende de uma etapa que acontece **fora** do ciclo de vida da
requisição HTTP: o consumo da mensagem do Kafka pelo Worker. Essa etapa envolve, no
mínimo:

1. O Producer (API) publicar a mensagem no broker Kafka.
2. O broker Kafka persistir a mensagem no tópico/partição.
3. O Consumer (Worker) fazer o próximo `poll` e receber a mensagem — o Worker consome em
   loop, não é notificado instantaneamente.
4. O Worker desserializar o evento, carregar a transação e as contas do PostgreSQL,
   aplicar as regras de negócio e persistir o novo status.

Cada uma dessas etapas tem uma latência própria (rede, I/O em disco do Kafka, tempo de
`poll`, round-trip ao PostgreSQL). Somadas, elas garantem que sempre existirá algum atraso
entre a criação da transação e sua conclusão — por isso o frontend precisa consultar o
status depois, em vez de recebê-lo pronto na resposta do POST.

## Como o Blazor lida com isso: polling

Depois do POST, `Home.razor` inicia um laço de polling
(`PollStatusAsync` em [Home.razor](../src/FinancialTransaction.Web/Components/Pages/Home.razor)):

```text
a cada 2s, por até 30s:
    GET /api/transactions/{id}
    atualiza o chip de status na tela (Pending / Processing / Processed / Failed)
    se status é Processed ou Failed -> para o polling
```

É uma solução propositalmente simples (sem SignalR, sem WebSocket, sem Server-Sent
Events) — adequada para o objetivo didático desta fase. Alternativas mais sofisticadas
(notificação push do servidor quando o Worker termina) ficam fora de escopo aqui.

---
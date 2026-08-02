# FinancialTransaction — Fase 4: API REST

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 4 — API REST

## Objetivo

Criar a API HTTP.

Endpoints:

```http
POST /api/transactions
GET /api/transactions/{id}
GET /api/transactions
```

Fluxo:

```text
HTTP
 │
 ▼
Controller/Endpoint
 │
 ▼
Application
 │
 ▼
Domain
 │
 ▼
Infrastructure
 │
 ▼
PostgreSQL
```

### Teste

Swagger/Postman.

### Definition of Done

- [x] POST funcionando;
- [x] GET por ID;
- [x] GET lista;
- [x] validação;
- [x] Swagger;
- [x] testes.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Nesta fase implemente a API REST usando ASP.NET Core .NET 10.

Endpoints:

POST /api/transactions
GET /api/transactions/{id}
GET /api/transactions

Explique:

1. Diferença entre Controller e Minimal API.
2. Como separar API de Application.
3. DTOs.
4. Validação.
5. Status HTTP.
6. Como tratar erros.

O POST deve:

1. Receber conta origem.
2. Receber conta destino.
3. Receber valor.
4. Validar.
5. Criar FinancialTransaction.
6. Persistir como Pending.
7. Retornar o ID e status.

Nesta fase NÃO publique Kafka.

Não implemente Worker.
Não implemente OpenTelemetry.

Crie testes unitários e de integração.

Ao final forneça exemplos de requisições HTTP e respostas esperadas.
```

---

## O que foi implementado

### Endpoints

- `POST /api/transactions` — cria uma transação como `Pending`. Retorna `201 Created` com o `Id`/`status` e o cabeçalho `Location`.
- `GET /api/transactions/{id}` — consulta uma transação. Retorna `200 OK` ou `404 Not Found`.
- `GET /api/transactions` — lista todas as transações. Retorna `200 OK`.

Implementados como Minimal API, agrupados via `MapGroup("/api/transactions")` (`src/FinancialTransaction.Api/Endpoints/TransactionEndpoints.cs`).

### Camada Application

Antes desta fase, a camada Application só tinha as abstrações de persistência (`IAccountRepository`, `IFinancialTransactionRepository`, `IUnitOfWork`), sem nenhum caso de uso. Foram adicionados:

- `Transactions/ITransactionService` / `TransactionService` — orquestra o caso de uso: busca as contas de origem/destino, chama `FinancialTransaction.Create` (domínio), persiste via repositório/UnitOfWork e mapeia para DTO.
- `Transactions/Dtos/CreateTransactionRequest` e `TransactionResponse` — DTOs para não expor as entidades de domínio na API.
- `Common/Exceptions/NotFoundException` — usada quando conta ou transação não são encontradas.
- `DependencyInjection.cs` (`AddApplication`) — registra os services no container de DI.

O endpoint HTTP não faz nenhuma lógica de negócio nem acesso a dados: só traduz a chamada para `ITransactionService` e converte o resultado/exceção em resposta HTTP.

### Validação e tratamento de erros

Três camadas de validação:

1. **Model binding**: JSON/tipos inválidos já retornam `400` automaticamente pelo ASP.NET Core.
2. **Existência de contas** (Application): `NotFoundException` quando a conta de origem/destino não existe.
3. **Invariantes de domínio** (Domain): `DomainException` quando o valor é ≤ 0 ou as contas são iguais (regras já existentes na entidade `FinancialTransaction`).

Um único `GlobalExceptionHandler` (`IExceptionHandler`, em `src/FinancialTransaction.Api/ExceptionHandling/`) centraliza o mapeamento de exceções para `ProblemDetails`:

| Exceção | Status |
|---|---|
| `NotFoundException` | 404 |
| `DomainException` / `ArgumentException` | 400 |
| qualquer outra | 500 |

### Swagger

Mantido o gerador nativo `Microsoft.AspNetCore.OpenApi` (`/openapi/v1.json`) já presente no scaffold, complementado pelo pacote `Swashbuckle.AspNetCore.SwaggerUI` apenas para a interface interativa, disponível em `/swagger` (ambiente Development).

### Seed de contas (Development)

Como esta fase não define endpoints de escrita para contas, foi criado `AccountsSeeder` (`src/FinancialTransaction.Infrastructure/Persistence/AccountsSeeder.cs`), chamado em `Program.cs` apenas quando `app.Environment.IsDevelopment()`:

- Se a tabela `accounts` estiver vazia, cria duas contas de teste (`ACC-001` e `ACC-002`) usando o próprio `Account.Create` do domínio.
- Loga os IDs gerados no console, para uso imediato nos exemplos de requisição.
- É idempotente: não duplica contas em execuções seguintes.

### Endpoint de listagem de contas

Para permitir descobrir os IDs das contas sem depender do log do console (e sem sair do escopo de "somente leitura" desta fase), foi adicionado:

- `GET /api/accounts` — lista todas as contas cadastradas. Retorna `200 OK`.

Seguindo o mesmo padrão em camadas das transações: `IAccountRepository.GetAllAsync` (Infrastructure), `Accounts/IAccountService` / `AccountService` / `Accounts/Dtos/AccountResponse` (Application), `Endpoints/AccountEndpoints.cs` (Api).

### Testes

- **Unitários** (`tests/FinancialTransaction.UnitTests/Application/`): `TransactionServiceTests` e `AccountServiceTests`, usando fakes em memória (`InMemoryAccountRepository`, `InMemoryFinancialTransactionRepository`, `NoOpUnitOfWork`) — sem biblioteca de mocking, seguindo o estilo já usado no projeto.
- **Integração** (`tests/FinancialTransaction.IntegrationTests/Api/`): `TransactionEndpointsTests` e `AccountEndpointsTests`, usando `WebApplicationFactory<Program>` + Testcontainers (Postgres real em container), cobrindo os fluxos de sucesso e erro (201/200/400/404) ponta a ponta via HTTP.

Resultado: 24 testes unitários e 10 testes de integração, todos passando.

### Fora do escopo desta fase (propositalmente)

- Não há endpoints de escrita para contas (POST/PUT) — apenas o seed de desenvolvimento e a listagem.
- Kafka, Worker e OpenTelemetry não foram tocados, conforme instruído no prompt da fase.

---
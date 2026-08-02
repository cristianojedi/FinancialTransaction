# FinancialTransaction — Fase 3: PostgreSQL + EF Core

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 3 — PostgreSQL + EF Core

## Objetivo

Adicionar persistência.

Infraestrutura Docker:

```text
PostgreSQL
```

Fluxo:

```text
API
 │
 ▼
Application
 │
 ▼
Infrastructure
 │
 ▼
EF Core
 │
 ▼
PostgreSQL
```

### Teste

Criar uma transação e verificar no PostgreSQL.

### Definition of Done

- [ ] PostgreSQL em Docker;
- [ ] EF Core configurado;
- [ ] migrations;
- [ ] banco criado;
- [ ] persistência funcionando;
- [ ] integração testada.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Nesta fase implemente somente persistência com PostgreSQL e Entity Framework Core.

Utilize Docker Compose para PostgreSQL.

Explique:

1. O que é DbContext.
2. O que é Migration.
3. Por que a configuração do EF Core fica em Infrastructure.
4. Como Application acessa persistência sem conhecer detalhes do banco.
5. Como configurar connection string.
6. Como executar migrations.

Implemente:

- DbContext.
- Configurações das entidades.
- Repositório ou abstração de persistência adequada.
- Migrations.
- Docker Compose do PostgreSQL.

Não implemente Kafka.
Não implemente Worker.
Não implemente OpenTelemetry.
Não implemente Blazor.

Crie testes de integração para validar persistência.

Ao final explique:

- Como subir PostgreSQL.
- Como criar banco.
- Como aplicar migration.
- Como verificar dados.
- Como executar testes.

Não avance para Kafka.
```

---

## Implementação realizada

### 1. Conceitos

**DbContext** — é a classe do EF Core que representa a sessão com o banco de dados. Ele mantém o *change tracker* (rastreia o que foi criado/alterado/removido em memória), expõe os `DbSet<T>` (uma coleção por tabela) e traduz operações LINQ em SQL. Aqui: `FinancialTransactionDbContext` (`src/FinancialTransaction.Infrastructure/Persistence/FinancialTransactionDbContext.cs`), com `DbSet<Account>` e `DbSet<FinancialTransaction>`.

**Migration** — é um arquivo de código gerado pelo EF Core que descreve, de forma incremental, como transformar o schema do banco (criar/alterar tabelas, índices, colunas) para refletir o estado atual das entidades mapeadas. Cada migration tem um método `Up` (aplica) e `Down` (reverte). A primeira migration deste projeto é `InitialCreate` (`src/FinancialTransaction.Infrastructure/Persistence/Migrations/`), que cria as tabelas `accounts` e `transactions`.

**Por que a configuração do EF Core fica em Infrastructure** — `Domain` não pode depender de detalhes técnicos (banco, ORM, SQL). `Infrastructure` é a camada responsável por implementações concretas de integração externa, então é lá que ficam o `DbContext`, os `IEntityTypeConfiguration<T>` (Fluent API) e os repositórios concretos. Isso mantém o domínio puro e testável sem infraestrutura.

**Como Application acessa persistência sem conhecer detalhes do banco** — `Application` define apenas abstrações (`IFinancialTransactionRepository`, `IAccountRepository`, `IUnitOfWork` em `src/FinancialTransaction.Application/Abstractions/Persistence/`). Essas interfaces não referenciam EF Core nem PostgreSQL. `Infrastructure` implementa essas interfaces usando o `DbContext`. A composição acontece via injeção de dependência (`AddInfrastructure` em `src/FinancialTransaction.Infrastructure/DependencyInjection.cs`), registrada no `Program.cs` da Api. Assim, `Application`/`Domain` nunca referenciam `Npgsql` ou `Microsoft.EntityFrameworkCore`.

**Connection string** — configurada em `src/FinancialTransaction.Api/appsettings.json`, na chave `ConnectionStrings:PostgreSql`. `DependencyInjection.AddInfrastructure` lê essa string via `IConfiguration` e configura o `DbContext` com `UseNpgsql(connectionString)`.

**Como executar migrations** — usando a CLI `dotnet-ef`, indicando o projeto que contém o `DbContext` (`--project`) e o projeto de inicialização, de onde vem a configuração (`--startup-project`):

```bash
dotnet ef migrations add NomeDaMigration --project src/FinancialTransaction.Infrastructure --startup-project src/FinancialTransaction.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/FinancialTransaction.Infrastructure --startup-project src/FinancialTransaction.Api
```

### 2. O que foi implementado

- `docker-compose.yml` na raiz do repositório, com o serviço `postgres` (imagem `postgres:16-alpine`, porta `5432`, volume nomeado e healthcheck).
- `FinancialTransaction.Application/Abstractions/Persistence/`: `IFinancialTransactionRepository`, `IAccountRepository`, `IUnitOfWork` — abstrações de persistência, sem dependência de EF Core.
- `FinancialTransaction.Infrastructure/Persistence/FinancialTransactionDbContext.cs` — o `DbContext`.
- `FinancialTransaction.Infrastructure/Persistence/Configurations/` — `AccountConfiguration` e `FinancialTransactionConfiguration` (Fluent API: chaves, tamanhos de coluna, precisão do `Amount`, conversão do enum `Status` para `string`, índices).
- `FinancialTransaction.Infrastructure/Persistence/Repositories/` — `AccountRepository` e `FinancialTransactionRepository`, implementações concretas das abstrações do `Application`.
- `FinancialTransaction.Infrastructure/Persistence/UnitOfWork.cs` — implementação de `IUnitOfWork` (encapsula `DbContext.SaveChangesAsync`).
- `FinancialTransaction.Infrastructure/DependencyInjection.cs` — extensão `AddInfrastructure(IConfiguration)` que registra o `DbContext` (Npgsql) e os repositórios no container de DI.
- `FinancialTransaction.Infrastructure/Persistence/Migrations/InitialCreate` — cria as tabelas `accounts` e `transactions`.
- `FinancialTransaction.Api/Program.cs` — chama `builder.Services.AddInfrastructure(builder.Configuration)`.
- `FinancialTransaction.Api/appsettings.json` — connection string `ConnectionStrings:PostgreSql`.
- `FinancialTransaction.IntegrationTests/Persistence/` — `PostgreSqlFixture` (sobe um container PostgreSQL efêmero via Testcontainers), `FinancialTransactionRepositoryTests` e `AccountRepositoryTests`, validando persistência real (insere, aplica migration, lê de volta com um `DbContext` novo).

Não foi implementado (fora de escopo desta fase, conforme instruído): Kafka, Worker, OpenTelemetry, Blazor.

### 3. Como executar

**Subir o PostgreSQL:**

```bash
docker compose up -d
docker compose ps
```

**Criar o banco:** o banco `financialtransaction` já é criado automaticamente pela imagem do Postgres (variável `POSTGRES_DB` no `docker-compose.yml`).

**Aplicar as migrations:**

```bash
dotnet ef database update --project src/FinancialTransaction.Infrastructure --startup-project src/FinancialTransaction.Api
```

**Verificar os dados** (via `psql` dentro do container, ou qualquer cliente PostgreSQL apontando para `localhost:5432`, usuário/senha/banco `financialtransaction`):

```bash
docker exec -it financialtransaction_postgres psql -U financialtransaction -d financialtransaction -c "\dt"
docker exec -it financialtransaction_postgres psql -U financialtransaction -d financialtransaction -c "SELECT * FROM transactions;"
```

**Executar os testes:**

```bash
dotnet test FinancialTransaction.slnx
```

Os testes de integração usam Testcontainers e sobem seu próprio container PostgreSQL automaticamente — é necessário apenas o Docker em execução, sem precisar do `docker compose up` previamente.

### 4. Definition of Done

- [x] PostgreSQL em Docker;
- [x] EF Core configurado;
- [x] migrations;
- [x] banco criado;
- [x] persistência funcionando;
- [x] integração testada.

Validado manualmente: uma transação inserida diretamente no banco subido via `docker compose` foi lida de volta com sucesso, confirmando o schema criado pela migration.

Não avançar para Kafka (Fase 4 é API REST).

---

## Resumo

A Fase 3 (PostgreSQL + EF Core) foi implementada:

- **`docker-compose.yml`** na raiz com serviço PostgreSQL 16 (healthcheck, volume persistente).
- **`Application`**: abstrações de persistência (`IFinancialTransactionRepository`, `IAccountRepository`, `IUnitOfWork`), sem dependência de EF Core.
- **`Infrastructure`**: `FinancialTransactionDbContext`, configurações Fluent API (`AccountConfiguration`, `FinancialTransactionConfiguration`), repositórios concretos, `UnitOfWork`, extensão de DI (`AddInfrastructure`) e a migration `InitialCreate`.
- **`Api`**: connection string em `appsettings.json` e `AddInfrastructure` registrado no `Program.cs`.
- **Testes de integração** (`FinancialTransaction.IntegrationTests`): usam Testcontainers para subir um PostgreSQL efêmero, aplicam a migration e validam a persistência real de contas e transações.

Validações feitas: build limpo (sem conflitos de versão do EF Core), `dotnet test` com **14 testes unitários + 3 de integração passando**, e uma verificação manual — subimos o Postgres via `docker compose up -d`, aplicamos a migration, inserimos uma transação e confirmamos a leitura via `psql`.

Não foi implementado Kafka, Worker, OpenTelemetry ou Blazor, conforme escopo desta fase.

---
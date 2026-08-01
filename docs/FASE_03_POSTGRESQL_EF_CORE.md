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
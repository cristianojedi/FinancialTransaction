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

- [ ] POST funcionando;
- [ ] GET por ID;
- [ ] GET lista;
- [ ] validação;
- [ ] Swagger;
- [ ] testes.

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
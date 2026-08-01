# FinancialTransaction — Fase 2: Domínio financeiro

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 2 — Domínio financeiro

## Objetivo

Criar o modelo de domínio.

Entidades:

```text
Account
FinancialTransaction
```

Enum:

```text
TransactionStatus
```

Eventos:

```text
TransactionCreated
TransactionProcessed
TransactionFailed
```

### Testes

Criar testes unitários para:

- valor maior que zero;
- conta origem diferente da conta destino;
- transação iniciando como Pending;
- transição de estados válida;
- transição de estados inválida.

### Definition of Done

- [ ] domínio implementado;
- [ ] invariantes implementadas;
- [ ] testes unitários;
- [ ] nenhum acesso a banco;
- [ ] nenhum Kafka.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

A solução utiliza:

FinancialTransaction.slnx

Projetos:

- Api
- Application
- Domain
- Infrastructure
- Worker
- Web
- UnitTests
- IntegrationTests

Nesta fase implemente somente o domínio financeiro.

Crie:

- Account
- FinancialTransaction
- TransactionStatus
- TransactionCreated
- TransactionProcessed
- TransactionFailed

Explique antes de implementar:

1. O que é uma entidade.
2. O que é uma Value Object, caso seja utilizada.
3. O que é uma regra de negócio.
4. Por que regras de domínio devem ficar no Domain.
5. Como representar estados de uma transação.

Regras mínimas:

- Valor deve ser maior que zero.
- Conta origem e destino devem ser diferentes.
- Nova transação inicia como Pending.
- Uma transação Pending pode ir para Processing.
- Processing pode ir para Processed ou Failed.
- Estados finais não podem voltar para Pending.

Crie testes unitários.

Não implemente:

- PostgreSQL.
- EF Core.
- Kafka.
- API.
- Worker.
- OpenTelemetry.

Ao final, explique cada classe criada e mostre como executar os testes.
```

---
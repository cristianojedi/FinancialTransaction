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

- [x] domínio implementado;
- [x] invariantes implementadas;
- [x] testes unitários;
- [x] nenhum acesso a banco;
- [x] nenhum Kafka.

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

## Fase 2 implementada

**Conceitos (resumo, conforme pedido pelo prompt da fase):**

1. **Entidade** — objeto com identidade própria (`Id`) que persiste ao longo do tempo, mesmo que seus atributos mudem. `Account` e `FinancialTransaction` são entidades.
2. **Value Object** — não usei um explícito nesta fase (mantive `Amount` como `decimal` puro para não expandir escopo). Um candidato natural seria `Money`, mas isso fica para quando a persistência (Fase 3) exigir formatação/moeda.
3. **Regra de negócio** — restrição que protege a consistência do domínio (ex.: valor > 0). Fica encapsulada nos métodos de fábrica/transição, nunca em camadas externas.
4. **Por que no Domain** — evita que regras financeiras vazem para Api/Infrastructure, garantindo que qualquer forma de criar/alterar uma transação (HTTP, Worker, teste) passe pelas mesmas invariantes.
5. **Estados da transação** — enum `TransactionStatus` (Pending → Processing → Processed/Failed), com transições validadas dentro do próprio agregado.

**Estrutura criada** em `src/FinancialTransaction.Domain`:
- `Common/Entity.cs`, `Common/AggregateRoot.cs`, `Common/IDomainEvent.cs` — infraestrutura mínima de domínio.
- `Exceptions/DomainException.cs` — exceção para violação de invariantes.
- `Enums/TransactionStatus.cs` — Pending, Processing, Processed, Failed.
- `Entities/Account.cs` — entidade simples com número de conta.
- `Entities/FinancialTransaction.cs` — agregado raiz com `Create`, `StartProcessing`, `CompleteProcessing`, `FailProcessing`, validando todas as invariantes mínimas e disparando os eventos.
- `Events/TransactionCreated.cs`, `TransactionProcessed.cs`, `TransactionFailed.cs`.

**Testes** em `tests/FinancialTransaction.UnitTests/Domain/` (`AccountTests.cs`, `FinancialTransactionTests.cs`), cobrindo: valor > 0, contas diferentes, início como Pending, transições válidas e inválidas, estados finais não retornando a Pending.

Nenhum acesso a banco, Kafka ou EF Core foi adicionado — escopo restrito à Fase 2.

**Validação:**

```bash
dotnet build FinancialTransaction.slnx   # compilação com êxito
dotnet test tests/FinancialTransaction.UnitTests/FinancialTransaction.UnitTests.csproj
# Aprovado! 14/14, 0 falhas
```

Conforme instrução do prompt da fase, não avancei para a Fase 3 (PostgreSQL/EF Core).

---
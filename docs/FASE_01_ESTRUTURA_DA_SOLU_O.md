# FinancialTransaction — Fase 1: Estrutura da solução

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 1 — Estrutura da solução

## Objetivo

Criar a solução utilizando `FinancialTransaction.slnx`.

Projetos:

```text
FinancialTransaction.Api
FinancialTransaction.Application
FinancialTransaction.Domain
FinancialTransaction.Infrastructure
FinancialTransaction.Worker
FinancialTransaction.Web
```

Testes:

```text
FinancialTransaction.UnitTests
FinancialTransaction.IntegrationTests
```

### Arquitetura

```text
                 FinancialTransaction.Web
                         │
                         ▼
                 FinancialTransaction.Api
                         │
                         ▼
                FinancialTransaction.Application
                         │
                         ▼
                  FinancialTransaction.Domain
                         ▲
                         │
             FinancialTransaction.Infrastructure


                 FinancialTransaction.Worker
                         │
                         ▼
                FinancialTransaction.Application
                         │
                         ▼
                  FinancialTransaction.Domain
```

### Objetivo arquitetural

A camada `Domain` não deve depender de infraestrutura.

A camada `Application` concentra casos de uso.

A `Infrastructure` implementa persistência e integrações.

A `Api` expõe HTTP.

O `Worker` processa mensagens assíncronas.

O `Web` é a interface do usuário.

### Teste

```bash
dotnet build FinancialTransaction.slnx
```

### Definition of Done

- [ ] `.slnx` criado;
- [ ] projetos criados;
- [ ] referências corretas;
- [ ] solução compilando;
- [ ] testes executando.

### Prompt para IA

```text
Atue como arquiteto especialista em .NET 10 e Clean Architecture.

Crie a estrutura inicial de uma solução chamada FinancialTransaction.

IMPORTANTE:
A solução deve utilizar o formato moderno:

FinancialTransaction.slnx

Não utilize FinancialTransaction.sln.

Projetos:

src/
- FinancialTransaction.Api
- FinancialTransaction.Application
- FinancialTransaction.Domain
- FinancialTransaction.Infrastructure
- FinancialTransaction.Worker
- FinancialTransaction.Web

tests/
- FinancialTransaction.UnitTests
- FinancialTransaction.IntegrationTests

Explique detalhadamente:

1. Por que cada projeto existe.
2. Qual responsabilidade cada projeto terá.
3. Como as dependências devem ser organizadas.
4. Por que Domain não deve depender de Infrastructure.
5. Como Worker e API compartilham Application e Domain.

Crie apenas a estrutura inicial.

Não implemente banco.
Não implemente Kafka.
Não implemente OpenTelemetry.
Não implemente regras de negócio.

Ao final:

1. Mostre a árvore de diretórios.
2. Execute ou indique o comando para compilar.
3. Execute ou indique o comando para rodar os testes.
4. Explique o resultado esperado.

Não avance para a próxima fase.
```

---
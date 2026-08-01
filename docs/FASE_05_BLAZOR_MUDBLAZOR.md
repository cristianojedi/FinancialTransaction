# FinancialTransaction — Fase 5: Blazor + MudBlazor

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 5 — Blazor + MudBlazor

## Objetivo

Criar uma interface simples para iniciar o fluxo.

Tela:

```text
┌───────────────────────────────────────────────┐
│       PROCESSAMENTO DE TRANSAÇÃO              │
│                                               │
│ Conta origem                                  │
│ [ ACC-001                              ]      │
│                                               │
│ Conta destino                                 │
│ [ ACC-002                              ]      │
│                                               │
│ Valor                                         │
│ [ R$ 1.500,00                          ]      │
│                                               │
│       [ CRIAR TRANSAÇÃO ]                     │
│                                               │
│ Status: Pending                               │
└───────────────────────────────────────────────┘
```

Fluxo:

```text
Blazor
  │
  │ HTTP
  ▼
API
  │
  ▼
PostgreSQL
```

### Definition of Done

- [ ] Blazor funcionando;
- [ ] MudBlazor configurado;
- [ ] formulário;
- [ ] validação;
- [ ] chamada HTTP;
- [ ] exibição de resultado.

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Nesta fase implemente somente o frontend Blazor com MudBlazor.

Projeto:

FinancialTransaction.Web

Crie uma tela para criar uma transação financeira.

Campos:

- Conta origem.
- Conta destino.
- Valor.

Utilize componentes MudBlazor.

A tela deve:

1. Validar campos.
2. Validar valor maior que zero.
3. Impedir contas iguais.
4. Chamar POST /api/transactions.
5. Exibir o ID retornado.
6. Exibir o status Pending.
7. Exibir mensagens de erro amigáveis.
8. Ter estado de carregamento.

Explique:

1. Como Blazor chama uma API.
2. Como configurar HttpClient.
3. Como tratar erros HTTP.
4. Como organizar componentes.
5. Como usar MudBlazor.

Não implemente Kafka.
Não implemente Worker.
Não implemente OpenTelemetry.

O objetivo é criar o primeiro fluxo end-to-end:

Blazor -> API -> PostgreSQL.

Ao final forneça os passos para executar Web e API simultaneamente.
```

---
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
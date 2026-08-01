# FinancialTransaction — Fase 14: Logs estruturados

> Prompt de execução para Claude Code.
>
> **Como usar:** execute este prompt dentro da raiz do repositório `FinancialTransaction`.
> Leia o `PROJECT_GUIDE.md` antes de começar. Implemente somente esta fase.
> Ao concluir, execute os testes/validações descritos, atualize a documentação quando solicitado e pare.
> Não avance automaticamente para a próxima fase.

---

# Fase 14 — Logs estruturados

## Objetivo

Adicionar Serilog e correlação.

Exemplo:

```json
{
  "Timestamp": "2026-08-01T20:00:00Z",
  "Level": "Information",
  "Message": "Transaction processed",
  "TransactionId": "123",
  "TraceId": "abc123",
  "SpanId": "def456"
}
```

### Prompt para IA

```text
Continue o projeto FinancialTransaction.

Implemente logs estruturados utilizando Serilog.

Os logs devem permitir correlação por:

- TransactionId.
- TraceId.
- SpanId.

Explique:

1. Log estruturado.
2. Diferença entre log textual e estruturado.
3. Correlação.
4. Por que TraceId é importante.

Adicione logs relevantes em:

- API.
- Producer.
- Consumer.
- Worker.
- Processamento.
- Erros.

Evite logar dados financeiros sensíveis.

Mostre exemplos de logs esperados.
```

---
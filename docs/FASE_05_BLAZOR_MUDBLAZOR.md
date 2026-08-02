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

- [x] Blazor funcionando;
- [x] MudBlazor configurado;
- [x] formulário;
- [x] validação;
- [x] chamada HTTP;
- [x] exibição de resultado.

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

## O que foi implementado

### MudBlazor

Pacote `MudBlazor` (9.7.0) adicionado a `FinancialTransaction.Web.csproj`. Configuração:

- `Program.cs` — `builder.Services.AddMudServices()`.
- `Components/_Imports.razor` — `@using MudBlazor`.
- `Components/App.razor` — CSS (`MudBlazor.min.css`) e JS (`MudBlazor.min.js`) do pacote, servidos via `_content/MudBlazor/...` (convenção de Razor Class Library).
- `Components/Layout/MainLayout.razor` — providers globais do MudBlazor (`MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider`) e layout (`MudLayout`, `MudAppBar`, `MudMainContent`, `MudContainer`).

**Importante — interatividade global:** o scaffold original do template Blazor Web App declarava `.AddInteractiveServerRenderMode()` no `Program.cs`, mas não aplicava nenhum `@rendermode` em `App.razor`. Sem isso, todas as páginas eram renderizadas apenas como HTML estático (SSR), sem circuito SignalR — os componentes MudBlazor apareciam na tela, mas cliques não disparavam nenhum código C#. A correção foi declarar o modo de renderização no componente raiz:

```razor
<HeadOutlet @rendermode="InteractiveServer" />
...
<Routes @rendermode="InteractiveServer" />
```

Isso tornou toda a aplicação interativa (Blazor Server), o que é necessário para formulários, validação e chamadas HTTP client-side funcionarem.

### Tela de criação de transação

`Components/Pages/Home.razor` (rota `/`) contém o formulário completo:

- `MudSelect<Guid?>` para conta origem e conta destino, populados a partir de `GET /api/accounts`.
- `MudNumericField<decimal?>` para o valor, formatado em `pt-BR` com prefixo "R$".
- `MudForm` para validação dos campos obrigatórios (`Required`/`RequiredError`).
- Validações adicionais feitas manualmente no `SubmitAsync`: valor maior que zero e conta origem diferente da conta destino — exibidas em um `MudAlert` de erro amigável, sem expor detalhes técnicos.
- Estado de carregamento: botão desabilitado (`Disabled="_isSubmitting"`) com `MudProgressCircular` enquanto a chamada HTTP está em andamento.
- Resultado exibido em um `MudPaper` com o `Id` e o `Status` (`Pending`) retornados pela API, além de um `MudSnackbar` de sucesso.

### Como o Blazor chama a API

O projeto Web **não referencia** os projetos `Application`/`Domain` — ele é um cliente HTTP independente, com seus próprios modelos (`Web/Models/*.cs`) espelhando o contrato JSON da API (`AccountResponse`, `CreateTransactionRequest`, `TransactionResponse`).

A comunicação é feita por `IFinancialApiClient`/`FinancialApiClient` (`Web/Services/`), um typed `HttpClient`:

```csharp
builder.Services.AddHttpClient<IFinancialApiClient, FinancialApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});
```

Como o Blazor está em modo **Server** (não WebAssembly), essas chamadas HTTP acontecem no processo do servidor `FinancialTransaction.Web`, não no navegador — por isso não é necessário configurar CORS na API: do ponto de vista da API, a requisição vem de outro processo .NET, não de JavaScript rodando em `localhost:7184`.

### Configuração do HttpClient

A URL base da API vem de configuração (`appsettings.json` → `Api:BaseUrl`, hoje `https://localhost:7083`), lida uma vez no `Program.cs` e usada para configurar o `HttpClient` registrado via `AddHttpClient<TClient, TImplementation>` — o padrão recomendado pela Microsoft para clientes tipados, que já cuida do ciclo de vida do `HttpMessageHandler` (evita esgotamento de sockets) e permite injetar `IFinancialApiClient` diretamente nos componentes.

### Como os erros HTTP são tratados

`FinancialApiClient.EnsureSuccessAsync` centraliza o tratamento:

1. Se a resposta for de sucesso, retorna normalmente.
2. Caso contrário, tenta desserializar o corpo como `ProblemDetails` (formato retornado pelo `GlobalExceptionHandler` da API — ver Fase 4) e usa `Detail`/`Title` como mensagem amigável.
3. Se o corpo não for um `ProblemDetails` válido, cai para uma mensagem genérica com o código HTTP.
4. Lança `ApiException`, capturada nas páginas Razor e exibida em um `MudAlert`.

Falhas de conexão (API fora do ar) lançam `HttpRequestException`, tratada separadamente com a mensagem "Não foi possível conectar à API. Verifique se ela está em execução."

### Como os componentes estão organizados

```text
FinancialTransaction.Web/
├── Components/
│   ├── App.razor              # host HTML, CSS/JS do MudBlazor, @rendermode
│   ├── Routes.razor           # roteador
│   ├── Layout/
│   │   └── MainLayout.razor   # providers do MudBlazor + MudLayout/MudAppBar
│   └── Pages/
│       └── Home.razor         # tela de criação de transação ("/")
├── Models/                    # DTOs que espelham o contrato JSON da API
│   ├── AccountResponse.cs
│   ├── CreateTransactionRequest.cs
│   └── TransactionResponse.cs
└── Services/                  # cliente HTTP tipado
    ├── IFinancialApiClient.cs
    ├── FinancialApiClient.cs
    └── ApiException.cs
```

A separação `Models`/`Services` mantém a página Razor (`Home.razor`) focada em UI e orquestração; toda a lógica de comunicação HTTP e mapeamento de erros fica isolada e testável fora do componente.

### Fora do escopo desta fase (propositalmente)

- Kafka, Worker e OpenTelemetry não foram tocados, conforme instruído no prompt da fase.
- Não há consulta de status pós-criação (polling) — isso é da Fase 9.

## Como executar Web e API simultaneamente

Pré-requisitos: PostgreSQL rodando (`docker compose up -d postgres`, se ainda não estiver).

Em dois terminais separados, a partir da raiz do repositório:

```bash
# Terminal 1 — API (https://localhost:7083)
dotnet run --project src/FinancialTransaction.Api --launch-profile https

# Terminal 2 — Web (https://localhost:7184)
dotnet run --project src/FinancialTransaction.Web --launch-profile https
```

Acesse `https://localhost:7184`. A tela "Processamento de Transação" carrega as contas via `GET /api/accounts` (contas `ACC-001`/`ACC-002` já existem graças ao seed da Fase 4). Preencha conta origem, conta destino e valor, e clique em "CRIAR TRANSAÇÃO" — o `Id` e o `Status: Pending` retornados pela API são exibidos na tela.

A URL da API usada pelo Web fica em `src/FinancialTransaction.Web/appsettings.json` (`Api:BaseUrl`); ajuste caso a API rode em outra porta.

---
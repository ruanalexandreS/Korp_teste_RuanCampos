# Korp NF — Sistema de Emissão de Notas Fiscais

Teste técnico para vaga de Estágio de Desenvolvimento | C# + Angular — Korp Informática.

## Tecnologias

**Frontend**

- Angular 17+ (Standalone Components)
- Angular Material
- RxJS (switchMap, catchError, BehaviorSubject)
- TypeScript

**Backend**

- .NET 8 — Minimal APIs
- Entity Framework Core 8
- LINQ
- PostgreSQL

**Infra**

- Docker Compose (Podman)
- GitHub Actions (CI)

## Arquitetura

Dois microsserviços independentes com bancos de dados separados:

- **StockService** (porta 5263) — cadastro de produtos e controle de saldo
- **BillingService** (porta 5199) — gestão de notas fiscais, comunicação HTTP com StockService

## Como rodar

### Pré-requisitos

- .NET 8 SDK
- Node.js 18+
- Docker ou Podman

### 1. Subir os bancos

```bash
podman compose up -d
```

### 2. StockService

```bash
cd services/StockService/StockService
dotnet ef database update
dotnet run
```

### 3. BillingService

```bash
cd services/BillingService/BillingService
dotnet ef database update
dotnet run
```

### 4. Frontend

```bash
cd frontend
npm install
ng serve
```

Acesse: http://localhost:4200

## Funcionalidades

- Cadastro de produtos com código, descrição e saldo
- Cadastro de notas fiscais com múltiplos produtos
- Numeração sequencial automática
- Impressão de NF: atualiza status para Fechada e desconta saldo dos produtos
- Tratamento de falha: se StockService estiver indisponível, BillingService retorna erro 503 com mensagem clara

## Detalhamento técnico

**Ciclos de vida Angular:** `ngOnInit` para carregamento de dados, `ngOnDestroy` via `takeUntilDestroyed`

**RxJS:** `switchMap` no fluxo de impressão, `catchError` para tratamento de falhas HTTP

**LINQ:** utilizado no StockService para consultas com `Where`, `Select` e `MaxAsync`

**Tratamento de erros:** middleware global no .NET, retorno de `Results.Problem` com status codes semânticos

**Idempotência:** verificação de status antes de processar impressão — nota já Fechada retorna BadRequest sem reprocessar

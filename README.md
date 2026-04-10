# Korp NF — Sistema de Emissão de Notas Fiscais

Teste técnico para vaga de Estágio de Desenvolvimento | C# + Angular — Korp Informática.

## Tecnologias

**Frontend**

- Angular 17+ (Standalone Components, Zoneless)
- Angular Material
- RxJS (catchError, map)
- TypeScript
- Groq API (llama-3.3-70b-versatile)

**Backend**

- .NET 8 — Minimal APIs
- Entity Framework Core 8
- LINQ
- PostgreSQL

**Infra**

- Docker Compose (Podman)

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
cp .env.example .env
# Edite o .env e adicione sua chave da Groq
npm install
ng serve
```

Acesse: http://localhost:4200

## Funcionalidades

- Cadastro de produtos com código, descrição e saldo
- Sugestão de descrição via IA (Groq) ao cadastrar produto
- Cadastro de notas fiscais com múltiplos produtos
- Numeração sequencial automática
- Impressão de NF: atualiza status para Fechada e desconta saldo dos produtos
- Resumo automático da NF gerado por IA após impressão
- Alerta de estoque baixo gerado por IA na tela de produtos
- Tratamento de falha: se StockService estiver indisponível, BillingService retorna erro com mensagem clara

## Requisitos opcionais implementados

- **Tratamento de Concorrência:** lock pessimista com `FOR UPDATE` no PostgreSQL
- **Idempotência:** verificação de status antes de processar impressão + `Idempotency-Key` header
- **Inteligência Artificial:** 3 funcionalidades usando Groq API

## Detalhamento técnico

**Ciclos de vida Angular:** `ngOnInit` para carregamento de dados

**RxJS:** `catchError` para tratamento de falhas HTTP, `map` para transformação de respostas

**LINQ:** utilizado no StockService para consultas com `Where`, `Select` e `MaxAsync`

**Tratamento de erros:** `try/catch` no BillingService com retorno de `Results.Problem` e status codes semânticos

**Idempotência:** verificação de status antes de processar impressão — nota já Fechada retorna BadRequest sem reprocessar. Header `Idempotency-Key` para evitar processamento duplicado

**Concorrência:** `SELECT FOR UPDATE` garante que dois requests simultâneos não causem saldo negativo

**IA:** Groq API com modelo llama-3.3-70b-versatile para sugestão de descrição, resumo de NF e alerta de estoque baixo

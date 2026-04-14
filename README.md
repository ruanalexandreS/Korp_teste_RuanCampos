## 🔗 Links Rápidos
| | |
|---|---|
| 🌐 **Frontend** | https://korp-teste-ruan-campos.vercel.app |
| ⚙️ **StockService (Swagger)** | https://korpstockdb-production.up.railway.app/swagger |
| 🧾 **BillingService (Swagger)** | https://korpbillingdb-production.up.railway.app/swagger |

# Korp NF — Sistema de Emissão de Notas Fiscais

Teste técnico para vaga de Estágio de Desenvolvimento | C# + Angular — Korp Informática.

## Tecnologias

**Frontend**

- Angular 17+ (Standalone Components, Zoneless via `provideZonelessChangeDetection()`)
- Angular Material
- RxJS (map, takeUntil, Subject, Observable)
- TypeScript

**Backend**

- .NET 8 — Minimal APIs
- Entity Framework Core 8
- LINQ
- Polly (retry com backoff exponencial)
- PostgreSQL (Neon)

**Infra**

- Docker Compose (Podman)
- Railway (deploy dos microsserviços)
- Vercel (deploy do frontend)

## Arquitetura

Dois microsserviços independentes com bancos de dados separados:

- **StockService** (porta 5263) — cadastro de produtos, controle de saldo e integração com IA
- **BillingService** (porta 5199) — gestão de notas fiscais, comunicação HTTP com StockService e integração com IA

## Como rodar

**Pré-requisitos**

- .NET 8 SDK
- Node.js 18+
- Docker ou Podman (docker-compose.yml compatível com ambos)

> As migrations do Entity Framework já estão incluídas no repositório; o comando `database update` aplicará o esquema automaticamente.

**1. Subir os bancos**

```
podman compose up -d
```

**2. StockService**

```
cd services/StockService/StockService
dotnet ef database update
dotnet run
```

**3. BillingService**

```
cd services/BillingService/BillingService
dotnet ef database update
dotnet run
```

**4. Frontend**

```
cd frontend
cp .env.example .env
npm install
ng serve
```

Acesse: http://localhost:4200

## 🔒 Segurança
As chaves da Groq API e Strings de Conexão são injetadas exclusivamente via Variáveis de Ambiente em produção (Railway), seguindo boas práticas do **12-Factor App**. Nenhuma credencial está exposta no repositório.

## Funcionalidades

- Cadastro de produtos com código, descrição e saldo
- Validação de campos obrigatórios no frontend e no backend
- Sugestão de descrição via IA (Groq) ao cadastrar produto
- Alerta de estoque baixo gerado por IA na tela de produtos
- Cadastro de notas fiscais com múltiplos produtos
- Numeração sequencial automática com proteção contra race condition
- Impressão de NF: atualiza status para Fechada e decrementa saldo dos produtos
- Resumo da NF gerado por IA após impressão
- Rollback de compensação (Saga): se um débito falhar, os anteriores são revertidos
- Tratamento de falhas: mensagem amigável ao usuário quando StockService está indisponível

## ⭐ Diferenciais Técnicos
- **Rollback de Compensação (Saga Pattern):** se um débito de saldo falhar durante a impressão de NF com múltiplos produtos, todos os débitos anteriores são revertidos automaticamente
- **Lock Pessimista (`SELECT ... FOR UPDATE`):** impede saldo negativo em requisições concorrentes no PostgreSQL
- **Idempotência na Impressão:** segunda requisição de impressão da mesma NF é bloqueada sem efeito colateral
- **3 Integrações de IA via Groq:** sugestão de descrição, alerta de estoque e resumo de NF — todas exclusivamente pelo backend

## Requisitos opcionais implementados

- **Concorrência:** lock pessimista com `SELECT ... FOR UPDATE` no PostgreSQL
- **Idempotência:** chave fixa `print-{id}` via `Idempotency-Key` header + `ConcurrentDictionary` no backend
- **Inteligência Artificial:** 3 funcionalidades usando Groq API (llama-3.3-70b-versatile), integradas exclusivamente via backend

## Detalhamento técnico

**Ciclos de vida Angular:** `OnInit` para carregamento de dados; `OnDestroy` em todos os componentes com `destroy$` e `takeUntil` para cancelamento de subscriptions

**RxJS:** `Subject` e `takeUntil` para gerenciamento de ciclo de vida; `map` para transformação de respostas; `Observable` como padrão em todos os serviços HTTP

**LINQ:** `ToListAsync`, `AnyAsync`, `MaxAsync`, `Include`, `FindAsync` e `FromSqlRaw().FirstOrDefaultAsync` nos dois microsserviços

**Tratamento de erros:** `try/catch` em todos os endpoints com retorno de `Results.BadRequest`, `Results.NotFound`, `Results.Conflict` e `Results.Problem` conforme o contexto; `logger.LogError` para rastreabilidade

**Resiliência:** Polly com 3 tentativas e backoff exponencial (2s, 4s, 8s) nas chamadas HTTP entre microsserviços (Billing → Stock), protegendo a comunicação inter-serviços contra falhas transitórias

**Idempotência:** chave de impressão fixa por nota; segunda requisição bloqueada no backend sem efeito colateral

**Concorrência:** `SELECT ... FOR UPDATE` garante que dois requests simultâneos não causem saldo negativo

**IA:** Groq API com modelo llama-3.3-70b-versatile para sugestão de descrição, alerta de estoque baixo e resumo de nota fiscal; processamento assíncrono do ponto de vista do frontend (UX não bloqueante); chave de API exclusivamente no backend

# Projeto Blite — .NET + Angular (convenções)

> Mantém este arquivo curto (max ~30 linhas de regras não-óbvias). Claude já conhece o básico de DI, signals, etc. Foca no que é específico do projeto.

## Workflow .NET
IMPORTANT: Prefer retrieval-led reasoning over pretraining for any .NET work.
Workflow: skim repo patterns -> consult dotnet-skills by name -> implement smallest-change -> note conflicts.
Routing:
- C# / code quality: modern-csharp-coding-standards, csharp-concurrency-patterns, api-design, type-design-performance
- ASP.NET Core / Web: aspire-configuration, aspire-integration-testing
- Testing: testcontainers-integration-tests

## Clean Architecture — Blite standard

Layers (dependency direction: outer → inner, never reverse):
1. Domain (core)          — entities, value objects, domain events, domain services
2. Application            — use cases (MediatR handlers), DTOs, interfaces de infra
3. Infrastructure         — EF Core, external APIs, messaging, cache
4. Presentation (WebAPI)  — controllers/minimal APIs, filters, middleware

Rules:
- Domain has ZERO dependencies on other layers.
- Application depends only on Domain.
- Infrastructure implements interfaces declared in Application.
- Presentation orchestrates Application via MediatR.

Patterns:
- CQRS via MediatR (Commands return Result<T>, Queries return DTOs directly)
- Result<T> for recoverable errors, exceptions only for truly exceptional
- Specification pattern for complex queries
- Domain events via INotification, dispatched after DB commit

## Security baseline (non-negotiable)
- All user input goes through FluentValidation before hitting domain
- Parameterized queries only (Dapper com parâmetros nomeados, EF Core LINQ)
- Authentication: JWT com refresh rotation. Never store tokens em localStorage — httpOnly cookies
- CSRF tokens em todas as mutations (ASP.NET antiforgery)
- CORS explícito, nunca `*`
- Secrets via Azure Key Vault ou User Secrets em dev. Nunca hardcoded.
- Logging: nunca logar PII, tokens, senhas, CPF, cartão

## Observability
- Logging: Serilog com structured logging. Use message templates, never string interpolation.
- Tracing: ActivitySource por feature. Span names <20 chars, kebab-case.
- Metrics: System.Diagnostics.Metrics. Counter/Histogram/Gauge — sem Timer custom.
- Never log: passwords, tokens, PII, credit cards, CPFs, API keys
- Sampling: head-based 100% em dev, tail-based ou 10% em prod
- Correlation ID em TODO log. Propagar via traceparent.

## Angular project conventions
- Use standalone components only. No NgModules.
- State: signals + computed. Use NgRx only when cross-feature state is needed.
- Forms: Signal Forms (v21+) or ReactiveForms.
- HTTP: HttpClient with interceptors for auth, logging, error handling.
- Testing: Jest + Testing Library. E2E: Playwright.
- Style: Tailwind + component-scoped SCSS for complex cases.

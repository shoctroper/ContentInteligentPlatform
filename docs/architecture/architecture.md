# Arquitectura — Content Intelligence Platform (MVP)

## C4 Nivel 1 — Contexto

```
[Mario (usuario)] --HTTP/Swagger--> [Content Intelligence API]
[Content Intelligence API] --HTTPS--> [Claude API (Anthropic)]
[Content Intelligence API] --lee/escribe--> [knowledge/ (Git, Markdown)]
[Content Intelligence API] --lee/escribe--> [SQLite]
```

## C4 Nivel 2 — Contenedores

- **Api** (ASP.NET Core Minimal API): expone `/api/profiles`, `/api/generations`. Traduce excepciones de dominio a `ProblemDetails`.
- **Application** (CQRS/MediatR): `GenerateScriptCommand`, `CreateProfileCommand`, `GetGenerationHistoryQuery`, `RateGenerationCommand`. Orquesta el pipeline de 8 etapas del PRD (comprensión → extracción → validación → contexto → planeación → redacción → autoevaluación → salida), simplificado en el MVP a 3 llamadas reales a IA (comprensión+extracción, redacción, autoevaluación) para no disparar 8 requests por generación.
- **Domain**: entidades `Profile`, `Source`, `NewsItem`, `Generation`; value object `Script`; servicio de dominio `ProfileInheritanceResolver`.
- **Infrastructure**: `EfCoreDbContext` (SQLite), `MarkdownKnowledgeRepository` (lee `knowledge/profiles/*.md`), `ClaudeAiProvider` (adapter `IAiProvider`).

## Riesgos cubiertos por esta arquitectura
- R1 (alucinaciones): etapa de autoevaluación obligatoria antes de persistir; `Confidence` y `MissingInformation` viajan hasta el DTO de salida.
- R2 (acoplamiento a Claude): `IAiProvider` en `Application`, cero referencias a Anthropic fuera de `Infrastructure`.
- R3 (costo): `Generation` persiste tokens y costo en cada llamada.
- R6 (infra): sin dependencias de host específico; Dockerizable desde el día 1.

## Gate
Normalmente este documento requeriría aprobación humana antes de pasar a `developer`. El usuario indicó que no podrá interactuar por el momento y pidió avanzar de forma autónoma — se documenta el auto-approval aquí y las decisiones quedan abiertas a revisión posterior.

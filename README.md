# Content Intelligence Platform

Plataforma que transforma cualquier fuente de información en contenido optimizado para redes sociales usando IA,
sin acoplarse a un proveedor específico. El conocimiento editorial vive en archivos Markdown versionables (`knowledge/`),
nunca dentro del modelo. Ver PRD y documentación de proceso en `docs/`.

## Estado actual (MVP backend)

- .NET 8, Clean Architecture (Domain / Application / Infrastructure / Api), DDD + CQRS (MediatR), FluentValidation.
- Persistencia: EF Core + SQLite, con migración inicial (`InitialCreate`) aplicada automáticamente al arrancar.
- IA: proveedor intercambiable (`IAiProvider`); adapter para Claude (Anthropic) listo, solo falta la API key.
- Conocimiento editorial: `knowledge/*.md` con front-matter YAML (herencia entre perfiles).
- Observabilidad: OpenTelemetry (traces/metrics), correlation-id, health checks `/health` y `/ready`.
- Docker: `Dockerfile` multi-stage + `docker-compose.yml` listos para levantar localmente.
- CI: GitHub Actions (build + test + cobertura en cada push/PR, imagen a GHCR al mergear a `main`).
- 49 pruebas automatizadas, ~92% de cobertura de líneas (ver `docs/quality-gate.json`).
- Frontend Angular: pendiente (ver `docs/architecture/ADR-005-frontend-deferred.md`).

## Estructura

```
docs/
  PRD.md
  analysis.md              # historias técnicas, event storming, riesgos, estimación
  quality-gate.json         # resultado de la fase tester
  architecture/             # ADRs, data-model.json, openapi.yaml, architecture.md
  devops/                   # runbooks de deploy y rollback
src/
  Domain/  Application/  Infrastructure/  Api/
tests/
  Domain.Tests/  Application.Tests/  Infrastructure.Tests/  Api.IntegrationTests/
knowledge/
  identity.md  rules.md  profiles/*.md
Dockerfile
docker-compose.yml
.github/workflows/ci.yml
implementation.log.md       # decisiones, desviaciones y pendientes, iteración a iteración
```

## Correrlo localmente (.NET)

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Api
# Swagger: https://localhost:<puerto>/swagger
# Health:  http://localhost:<puerto>/health  y  /ready
```

Antes de generar contenido real:
```bash
cd src/Api
dotnet user-secrets init
dotnet user-secrets set "AiProvider:Claude:ApiKey" "sk-ant-..."
```

## Correrlo con Docker

```bash
export CLAUDE_API_KEY="sk-ant-..."
docker compose up -d
curl http://localhost:8080/health
```

Runbooks completos en `docs/devops/deploy.md` y `docs/devops/rollback.md`.

## Pendientes conocidos

Ver `implementation.log.md` para el detalle completo y priorizado.

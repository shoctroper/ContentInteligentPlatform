# Content Intelligence Platform

Plataforma que transforma cualquier fuente de información en contenido optimizado para redes sociales usando IA,
sin acoplarse a un proveedor específico. El conocimiento editorial vive en archivos Markdown versionables (`knowledge/`),
nunca dentro del modelo. Ver PRD y documentación de proceso en `docs/`.

## Estado actual (MVP backend)

- .NET 8, Clean Architecture (Domain / Application / Infrastructure / Api), DDD + CQRS (MediatR), FluentValidation.
- Persistencia: EF Core + SQLite, con migración inicial (`InitialCreate`) aplicada automáticamente al arrancar.
- IA: proveedor intercambiable (`IAiProvider`), seleccionable por configuración (`AiProvider:Active`); adapters listos para Claude (Anthropic), DeepSeek, OpenRouter y Gemini — **generación real validada end-to-end con Gemini**.
- Conocimiento editorial: `knowledge/*.md` con front-matter YAML (herencia entre perfiles). El system prompt combina 6 archivos base en orden fijo: `manifesto.md → identity.md → rules.md → thinking.md → storytelling.md → voice.md`.
- Observabilidad: OpenTelemetry (traces/metrics), correlation-id, health checks `/health` y `/ready`.
- Docker: `Dockerfile` multi-stage + `docker-compose.yml` listos para levantar localmente.
- CI: GitHub Actions (build + test + cobertura en cada push/PR, imagen a GHCR al mergear a `main`).
- 49 pruebas automatizadas, ~92% de cobertura de líneas (ver `docs/quality-gate.json`).
- Frontend Angular 18 (`frontend/`): formulario de generación + historial + calificación, funcional contra la Api. Ver ADR-006 (reactiva ADR-005).

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
  manifesto.md  identity.md  rules.md  thinking.md  storytelling.md  voice.md  profiles/*.md
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
dotnet user-secrets set "AiProvider:Active" "Claude"          # o "DeepSeek", "OpenRouter", "Gemini"
dotnet user-secrets set "AiProvider:Claude:ApiKey" "sk-ant-..."
dotnet user-secrets set "AiProvider:DeepSeek:ApiKey" "sk-..."
dotnet user-secrets set "AiProvider:OpenRouter:ApiKey" "sk-or-v1-..."
dotnet user-secrets set "AiProvider:Gemini:ApiKey" "AIza..."
```

## Correrlo con Docker

```bash
export CLAUDE_API_KEY="sk-ant-..."
docker compose up -d
curl http://localhost:8080/health
# Frontend: http://localhost:4200
```

## Correr el frontend en modo desarrollo

```bash
cd frontend
npm install
npm start          # http://localhost:4200, apunta a http://localhost:5080 (ver src/environments/environment.ts)
npm test           # requiere Chrome/Chromium disponible localmente
```

Runbooks completos en `docs/devops/deploy.md` y `docs/devops/rollback.md`.

## Pendientes conocidos

Ver `implementation.log.md` para el detalle completo y priorizado.

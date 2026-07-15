# Content Intelligence Platform

Plataforma que transforma cualquier fuente de información en contenido optimizado para redes sociales usando IA,
sin acoplarse a un proveedor específico. El conocimiento editorial vive en archivos Markdown versionables (`knowledge/`),
nunca dentro del modelo. Ver PRD y documentación de proceso en `docs/`.

## Estado actual (MVP backend)

- .NET 8, Clean Architecture (Domain / Application / Infrastructure / Api), DDD + CQRS (MediatR), FluentValidation.
- Persistencia: EF Core + SQLite.
- IA: proveedor intercambiable (`IAiProvider`); adapter inicial para Claude (Anthropic).
- Conocimiento editorial: `knowledge/*.md` con front-matter YAML (herencia entre perfiles).
- Frontend Angular: pendiente (ver `docs/architecture/ADR-005-frontend-deferred.md`).

## Estructura

```
docs/
  PRD.md
  analysis.md              # historias técnicas, event storming, riesgos, estimación
  architecture/             # ADRs, data-model.json, openapi.yaml, architecture.md
src/
  Domain/
  Application/
  Infrastructure/
  Api/
tests/
  Domain.Tests/
  Application.Tests/
  Api.IntegrationTests/
knowledge/
  identity.md
  rules.md
  profiles/*.md
```

## Correrlo localmente

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Api
# Swagger: https://localhost:<puerto>/swagger
```

Antes de generar contenido real, configura la API key de Claude:

```bash
cd src/Api
dotnet user-secrets init
dotnet user-secrets set "AiProvider:Claude:ApiKey" "sk-ant-..."
```

## Pendientes conocidos

Ver `implementation.log.md` para el detalle completo: cobertura de pruebas (~45-66%, objetivo 80%),
falta migración inicial de EF Core, falta Dockerfile/devops, falta frontend.

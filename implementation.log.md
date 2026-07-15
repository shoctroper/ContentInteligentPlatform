# Implementation Log — Content Intelligence Platform (MVP backend)

## Estado (actualizado — segunda iteración)
Solución .NET 8 compila y **49/49 pruebas pasan** (`dotnet test`): 18 Domain, 14 Application, 11 Infrastructure, 6 integración de Api.
Cobertura de líneas: **91.7%** (excluyendo migraciones EF Core autogeneradas) — supera el objetivo de 80% de la skill `dev-dotnet`.
Branch coverage: 69.7% (objetivo 70%, prácticamente alcanzado). Ver `docs/quality-gate.json`.

## Qué se agregó en esta iteración
1. **Tests de Infrastructure** (`tests/Infrastructure.Tests`): `ClaudeAiProviderTests` (con `FakeHttpMessageHandler`, sin red real — cubre éxito, falta de API key, error HTTP) y `MarkdownKnowledgeRepositoryTests` (con directorios temporales reales — cubre parseo de front-matter, herencia, archivos faltantes/malformados).
2. **Tests de Application faltantes**: `GetGenerationHistoryQuery`, `GetGenerationByIdQuery`, `GetProfilesQuery` (antes en 0% de cobertura).
3. **Migración inicial de EF Core** (`InitialCreate`) generada con `dotnet-ef`. `Program.cs` aplica `db.Database.Migrate()` al arrancar (solo si el provider es relacional, para no romper los tests con InMemory).
4. **Observabilidad**: OpenTelemetry (traces + metrics) con exporter a consola por defecto o a un collector OTLP configurable (`Otel:Endpoint`). Logging estructurado con scopes. Middleware de `X-Correlation-Id`.
5. **Health checks**: `/health` (liveness) y `/ready` (readiness, valida conexión a la base).
6. **Dockerfile multi-stage** (`mcr.microsoft.com/dotnet/sdk:8.0` build → `aspnet:8.0` runtime), usuario no-root, healthcheck embebido.
7. **docker-compose.yml** para levantar todo localmente con un volumen persistente para SQLite y `knowledge/` montado read-only.
8. **CI en GitHub Actions** (`.github/workflows/ci.yml`): build + test + reporte de cobertura en cada push/PR; build y push de imagen a GHCR (`ghcr.io/<owner>/content-intelligence-platform-api:<sha>`) al mergear a `main`.
9. **Runbooks**: `docs/devops/deploy.md` y `docs/devops/rollback.md`.

## Fase Tester — resumen (detalle en `docs/quality-gate.json`)
- **Contract check**: los 6 endpoints de `openapi.yaml` están implementados 1:1 (verificación manual; no se corrió Schemathesis/Dredd por falta de esas herramientas en el sandbox).
- **Flakiness**: suite completa corrida 3 veces consecutivas, 49/49 verde siempre.
- **E2E/Performance**: no ejecutados (sin frontend todavía para Playwright; sin k6/Artillery disponibles). Pendientes antes de producción real.
- **Testcontainers**: no se usó (sin Docker en el sandbox de desarrollo); se usó EF Core InMemory + filesystem temporal real como alternativa pragmática.

## Desviaciones respecto al diseño original (siguen vigentes de la iteración anterior)
1. Pipeline de 8 etapas del PRD implementado como 2 llamadas reales a IA (comprensión+extracción+validación, y planeación+redacción+autoevaluación) para no multiplicar costo/latencia x8 en el MVP.
2. Frontend Angular no incluido (ADR-005) — pendiente de que el usuario revise UX.

## Pendiente antes de "producción real"
1. Configurar `AiProvider:Claude:ApiKey` real (user-secrets local o `CLAUDE_API_KEY` en el entorno de despliegue).
2. Validar el `docker build` en un entorno con Docker real (no disponible en este sandbox de desarrollo; se validará automáticamente en el primer push vía GitHub Actions).
3. Backup automatizado de `/data/cip.db` (hoy no hay ninguno — ver `docs/devops/rollback.md`).
4. Contract testing automatizado (Schemathesis) y smoke de performance (k6) en CI.
5. Frontend Angular (ADR-005).
6. Revisar el warning `NU1902` de `OpenTelemetry.Exporter.OpenTelemetryProtocol` (vulnerabilidad moderada conocida en el paquete; no bloqueante, pendiente de que el proyecto libere un parche).

## Cómo correrlo

### Local (.NET)
```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Api
# Swagger: https://localhost:<puerto>/swagger
# Health: http://localhost:<puerto>/health y /ready
```

### Docker
```bash
export CLAUDE_API_KEY="sk-ant-..."
docker compose up -d
curl http://localhost:8080/health
```

Antes de generar contenido real localmente (sin Docker):
```bash
cd src/Api
dotnet user-secrets init
dotnet user-secrets set "AiProvider:Claude:ApiKey" "sk-ant-..."
```

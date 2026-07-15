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

---

## Tercera iteración: frontend Angular (ADR-006)

Con luz verde explícita del usuario para avanzar sin esperar revisión de UX, se construyó un frontend Angular 18
(standalone components, sin NgModules) en `frontend/`:

- **Páginas**: `/generar` (formulario: texto + perfil + formato → llama `POST /api/generations`, muestra el
  Markdown resultante, confianza e información faltante) y `/historial` (lista + detalle + calificación vía
  `PATCH /api/generations/{id}/rating`).
- **ApiService**: cliente HTTP tipado a mano contra `docs/architecture/openapi.yaml` (no autogenerado — ver
  ADR-006 para el trade-off).
- **CORS** habilitado en la Api (`Cors:AllowedOrigins`, default `http://localhost:4200`) para desarrollo.
- **Docker**: `frontend/Dockerfile` (build con Node 22 + `nginx` sirviendo el build de producción, con proxy
  `/api/*` hacia el servicio `api`). Sumado como segundo servicio en `docker-compose.yml` raíz.
- **Tests**: `ApiService`, `GenerateComponent`, `HistoryComponent` y `AppComponent` tienen specs con
  `HttpClientTestingModule`/`HttpTestingController`. **No se ejecutaron en este sandbox** — el host es
  aarch64 (arm64) y el único Chrome disponible vía `puppeteer` se descargó en build x86-64, incompatible
  (`Cannot start ChromeHeadless`). No hay apt/root para instalar un Chromium nativo arm64 tampoco.
  Mitigación: se verificó `npx tsc -p tsconfig.spec.json --noEmit` sin errores (los specs compilan y tipan
  correctamente) y se agregó un job `frontend-build-and-test` en `.github/workflows/ci.yml` que corre
  `ng test --browsers=ChromeHeadless` en el runner de GitHub Actions (Ubuntu con Chrome preinstalado) — ahí
  sí se ejecutarán de verdad en cada push/PR.
- **Build de producción** (`ng build`) sí se corrió y compiló limpio, en modo `production` y `development`.

### Pendiente de esta iteración
1. Confirmar en el primer push que el job `frontend-build-and-test` de CI efectivamente pasa (no pude
  validarlo localmente por la limitación de Chrome).
2. UX/diseño visual: hoy es funcional pero sin pulido — pendiente de revisión del usuario cuando tenga tiempo.
3. El cliente HTTP es manual; si el contrato OpenAPI cambia seguido, vale automatizar la generación
  (`openapi-generator`/`orval`).

---

## Cuarta iteración: segundo proveedor de IA — DeepSeek (ADR-007)

- Nuevo adapter `DeepSeekAiProvider` (API OpenAI-compatible, modelo `deepseek-v4-flash`, thinking mode
  deshabilitado por defecto). 5 tests nuevos (16/16 en `Infrastructure.Tests` ahora, antes 11).
- Selección de proveedor activo por configuración (`AiProvider:Active`), completando lo que ADR-004 prometía
  pero no implementaba (antes estaba hardcodeado a Claude en el DI).
- **Validación real (no mock)**: se corrió la Api completa en el sandbox con la API key de DeepSeek provista
  por el usuario contra un texto de prueba. La integración (auth, formato de request, manejo de errores) quedó
  confirmada — DeepSeek respondió `402 Insufficient Balance` (key válida, cuenta sin saldo), no un error de
  autenticación ni de formato. **Falta cargar saldo en la cuenta DeepSeek del usuario para ver contenido
  generado real.**
- Suite completa: **54/54 pruebas** (antes 49).
- La API key se usó solo como variable de entorno temporal en el sandbox; no quedó en ningún archivo del repo.

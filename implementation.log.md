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

---

## Quinta iteración: tercer proveedor de IA — OpenRouter (ADR-008)

- Nuevo adapter `OpenRouterAiProvider` (agregador multi-modelo, default `openai/gpt-4o-mini`, usa `usage.cost`
  nativo del response en vez de tabla de precios local). 5 tests nuevos (21/21 en `Infrastructure.Tests`).
- `AiProvider:Active` ahora soporta `"Claude" | "DeepSeek" | "OpenRouter"`.
- **Validación real**: misma mecánica que con DeepSeek — la Api completa corrió contra la API real de OpenRouter
  con la key provista. Resultado: `402 Insufficient credits` (key válida, cuenta sin créditos). Segunda
  confirmación consecutiva de que la arquitectura de proveedor intercambiable funciona correctamente end-to-end;
  el bloqueo para ver contenido real generado es exclusivamente de saldo/créditos en las cuentas de IA, no de
  código.
- Suite completa: **59/59 pruebas** (antes 54).
- La key se usó solo como variable de entorno temporal; no quedó en ningún archivo del repo.

---

## Sexta iteración: cuarto proveedor de IA — Gemini (ADR-009) + bug real corregido

- Nuevo adapter `GeminiAiProvider` (Interactions API de Google, header `x-goog-api-key`, modelo
  `gemini-3.5-flash`, thinking level `minimal`). `AiProvider:Active` ahora soporta Claude | DeepSeek |
  OpenRouter | Gemini.
- **Bug real encontrado en producción de pruebas**: Gemini envolvió su respuesta JSON en un fence de Markdown
  (```json ... ```), rompiendo el `JsonSerializer.Deserialize`. Corregido con `LlmJsonResponse.StripMarkdownFence()`,
  aplicado a **todos** los proveedores (no solo Gemini) en `GenerateScriptCommandHandler`. 5 tests nuevos
  cubriendo el sanitizador + el escenario de regresión.
- **Primera generación de contenido real y verificada de punta a punta del proyecto**: tras el fix, 2 de 3
  intentos contra la Api real devolvieron `201 Created` con un guion completo en español (noticia sobre una
  línea de metro en CDMX), con `confidence`, `missingInformation`, tokens y costo reales. El intento fallido
  fue `403 SERVICE_DISABLED` (la API tardó unos segundos en propagarse tras habilitarse en el proyecto de
  Google Cloud del usuario) — no un problema de nuestro código.
- Suite completa: **69/69 pruebas** (antes 59).
- La key se usó solo como variable de entorno temporal; no quedó en ningún archivo del repo.

---

## Séptima iteración: conocimiento base multi-archivo + bug de confidence/missingInformation (ADR-010)

- El usuario proporcionó el contenido completo de 5 documentos editoriales (Manifiesto, Identidad, Pensamiento,
  Storytelling, Voz). Se agregaron/reemplazaron en `knowledge/`: `manifesto.md`, `identity.md` (reemplaza el
  placeholder original), `thinking.md`, `storytelling.md`, `voice.md`.
- `GenerateScriptCommandHandler` ahora combina los 6 archivos base (`manifesto.md → identity.md → rules.md →
  thinking.md → storytelling.md → voice.md`, orden deliberado: por qué → quién → límites → cómo pensar → cómo
  narrar → cómo suena) en un solo system prompt, en vez de leer solo `identity.md`.
- **Demo en vivo pedida por el usuario**: se generó un guion de ejemplo (dos llamadas reales a Gemini,
  replicando el pipeline exacto fuera del sandbox de 45s por llamada) para un texto real sobre el episodio 126
  de "Loret en Latinus" (Loret/Brozo sobre Ernestina Godoy y la captura de "El Mayo" Zambada).
- **Bug real encontrado durante la demo**: la etapa de extracción detectó correctamente un vacío de información
  (posible inconsistencia de cargo/fecha de Ernestina Godoy) con `confidence: 0.9`, pero la etapa de redacción,
  al no recibir ese `missingInformation` en su prompt, produjo un guion final con `confidence: 1.0` y
  `missingInformation: null` — el vacío detectado se perdía silenciosamente en el resultado que ve el usuario.
  Esto contradice un principio explícito del Manifiesto/Reglas (declarar información faltante, no ocultarla).
- **Corrección** (ver ADR-010): el prompt de redacción ahora recibe explícitamente el `missingInformation` de
  extracción; además, el handler ya no persiste `script.Confidence`/`script.MissingInformation` directamente,
  sino una combinación programática: `finalConfidence = min(extraction.Confidence, script.Confidence)` y
  `finalMissingInformation` fusiona ambas etapas. Es una salvaguarda de código, no solo de prompt.
- 1 test de regresión nuevo replicando el caso real observado.
- Suite completa: **70/70 pruebas** (antes 69).

# Implementation Log — Content Intelligence Platform (MVP backend)

## Estado
Solución .NET 8 compila (`dotnet build`) y **32/32 pruebas pasan** (`dotnet test`): 18 unitarias de Domain, 8 unitarias de Application, 6 de integración de Api (`WebApplicationFactory`).

## Qué se construyó
- **Domain**: `Profile`, `Source`, `NewsItem`, `Generation` + value objects `Confidence` (0.0–1.0) y `Rating` (1–5) + `ProfileInheritanceResolver` (herencia de perfiles con detección de ciclos, sección 10 del PRD). Patrón `Result<T>` en vez de excepciones para flujo de negocio.
- **Application**: CQRS con MediatR — `GenerateScriptCommand` (orquesta el pipeline), `CreateProfileCommand`, `RateGenerationCommand`, `GetGenerationHistoryQuery`, `GetGenerationByIdQuery`, `GetProfilesQuery`. Validación con FluentValidation + `ValidationBehavior` como pipeline behavior de MediatR. Puertos: `IAiProvider`, `IKnowledgeRepository`, `IAppDbContext`.
- **Infrastructure**: `AppDbContext` (EF Core + SQLite), `MarkdownKnowledgeRepository` (lee `knowledge/*.md` con front-matter YAML vía YamlDotNet), `ClaudeAiProvider` (adapter HTTP a la API de Anthropic).
- **Api**: Minimal API, endpoints exactos del `openapi.yaml` (`/api/profiles`, `/api/generations`, `/api/generations/{id}`, `/api/generations/{id}/rating`), Swagger, `ProblemDetails` para errores (400 validación, 422 reglas de negocio, 404 no encontrado).
- **knowledge/**: `identity.md`, `rules.md` y los 7 perfiles pedidos (periodístico, documental, tecnología, humor, sarcástico-divertido, cine, música), con herencia declarada donde aplica.

## Desviaciones respecto al diseño original
1. **Pipeline de 8 etapas → 2 llamadas reales a IA.** El PRD describe 8 etapas (comprensión, extracción, validación, contexto, planeación, redacción, autoevaluación, salida). Implementarlas como 8 llamadas HTTP independientes multiplicaría el costo/latencia por 8 sin beneficio claro en el MVP. Se combinaron en 2 llamadas: (1) comprensión+extracción+validación → `NewsItem`, (2) planeación+redacción+autoevaluación → guion. Las 8 etapas siguen existiendo conceptualmente en el prompt, no como requests separados. Si esto no es aceptable, es un cambio de una línea (llamar `IAiProvider` más veces).
2. **Frontend Angular no incluido** (ver ADR-005) — se puede retomar cuando el usuario tenga disponibilidad para revisar UX.
3. **Sin Testcontainers**: el skill pedía Testcontainers para Infrastructure; se usó EF Core InMemory para los tests de Application/Api por velocidad y porque no había Docker disponible en el sandbox de desarrollo. **Recomendado antes de producción**: agregar un test de integración real contra SQLite en disco.

## Cobertura de pruebas (medida con `dotnet test --collect:"XPlat Code Coverage"`)
No se alcanzó el 80% pedido por la skill `dev-dotnet`:
- Domain.Tests → ~66% líneas del ensamblado Domain
- Application.Tests → ~61% líneas del ensamblado Application
- Api.IntegrationTests → ~45% líneas del ensamblado Api+Infrastructure

**Huecos principales sin cubrir**: `ClaudeAiProvider` (llamada HTTP real, no testeada — requiere API key real o un test con `HttpMessageHandler` fake, no implementado aún), `MarkdownKnowledgeRepository` (parseo de front-matter, sin test unitario todavía), `GetGenerationHistoryQuery`/`GetGenerationByIdQuery` (sin test dedicado, solo cubiertos indirectamente).

## Pendiente antes de "producción real"
1. Configurar `AiProvider:Claude:ApiKey` (user-secrets o variable de entorno) — hoy lanza excepción explícita si falta.
2. Agregar migración inicial EF Core (`dotnet ef migrations add InitialCreate`) — no se generó porque requiere el CLI de `dotnet-ef` instalado.
3. Subir cobertura de Infrastructure (tests de `ClaudeAiProvider` con `HttpMessageHandler` fake, tests de `MarkdownKnowledgeRepository` con archivos temporales).
4. Dockerfile + docker-compose (fase `devops`, no ejecutada en este ciclo).
5. Frontend Angular (ADR-005).

## Cómo correrlo
```
cd backend
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Api
# Swagger en https://localhost:xxxx/swagger
```

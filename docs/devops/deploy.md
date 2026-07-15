# Runbook — Deploy

## Prerrequisitos
- Docker y docker-compose instalados en el host destino.
- `knowledge/` presente junto al `docker-compose.yml` (se monta como volumen read-only).
- Variables de entorno: `CLAUDE_API_KEY` (obligatoria para generar contenido real), opcional `OTEL_ENDPOINT` si hay un collector OpenTelemetry corriendo.

## Build local

```bash
docker build -t content-intelligence-platform-api:local .
```

## Levantar con docker-compose

```bash
export CLAUDE_API_KEY="sk-ant-..."
docker compose up -d
```

La migración de EF Core (`InitialCreate`) se aplica automáticamente al arrancar (`db.Database.Migrate()` en `Program.cs`), no requiere paso manual.

## Healthchecks post-deploy

```bash
curl -f http://localhost:8080/health   # liveness: el proceso responde
curl -f http://localhost:8080/ready    # readiness: incluye chequeo de conexión a la DB
```
Si `/ready` falla pero `/health` responde, el proceso está vivo pero no puede hablar con la base de datos — revisar el volumen `cip-data` y permisos.

## Smoke test funcional

```bash
curl -s http://localhost:8080/api/profiles | jq .
# Debe listar al menos 0 perfiles sin error 500.
```

## Imagen desde CI (GHCR)

El workflow `.github/workflows/ci.yml` publica automáticamente en cada push a `main`:

```
ghcr.io/<owner>/content-intelligence-platform-api:<sha-del-commit>
ghcr.io/<owner>/content-intelligence-platform-api:latest
```

Usar siempre el tag por SHA en producción, nunca `latest` (regla de la skill devops).

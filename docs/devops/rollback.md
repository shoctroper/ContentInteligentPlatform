# Runbook — Rollback

## Si el deploy nuevo falla el smoke test

```bash
docker compose down
docker compose pull   # o: docker tag <sha-anterior> content-intelligence-platform-api:local
docker compose up -d
```

Como cada tag de imagen es el SHA del commit (ver `deploy.md`), volver a la versión anterior es
apuntar `docker-compose.yml` (o el orquestador que se use) al tag anterior conocido-bueno y reiniciar.

## Si el problema es la migración de base de datos

Las migraciones EF Core en este proyecto son aditivas en el MVP (no hay `DROP COLUMN`/`DROP TABLE` en `InitialCreate`).
Si una migración futura rompe algo:

```bash
# Desde un entorno con dotnet-ef y acceso al mismo archivo /data/cip.db:
dotnet ef database update <NombreDeLaMigracionAnterior> \
  --project src/Infrastructure --startup-project src/Api
```

Si la migración fue destructiva (pérdida de datos), restaurar desde el último backup de `/data/cip.db`
(no hay backup automatizado configurado todavía — pendiente antes de producción real, ver `implementation.log.md`).

## Verificación post-rollback

Repetir los healthchecks y el smoke test de `deploy.md` contra la versión restaurada.

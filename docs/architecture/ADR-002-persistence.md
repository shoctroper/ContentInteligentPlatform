# ADR-002: Persistencia

**Estado:** Aceptado

## Contexto
PRD pide SQLite en MVP, PostgreSQL en producción (sección 18). El conocimiento editorial (perfiles, reglas) vive en archivos Markdown, no en la base de datos (sección 9).

## Decisión
EF Core con provider SQLite para el MVP. La base de datos solo almacena **metadatos e historial** (Sources, NewsItems, Generations, índice de Profiles), nunca el conocimiento editorial en sí. Migraciones EF Core desde el día 1 para que el cambio a Npgsql (PostgreSQL) sea solo de configuración.

## Consecuencias
- Cero fricción para desarrollo local (SQLite = archivo único).
- Migración a Postgres = cambiar connection string + provider, sin tocar Domain/Application.

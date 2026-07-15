# ADR-003: Knowledge Engine (conocimiento editorial)

**Estado:** Aceptado

## Contexto
PRD sección 9 y 16: todo el conocimiento (identity, rules, perfiles) debe vivir en Markdown versionable, nunca dentro del modelo ni hardcodeado. Los perfiles heredan entre sí (sección 10) y se versionan sin sobrescribir (v1, v2, v3...).

## Decisión
Carpeta `knowledge/` en el filesystem (versionada con Git, fuera de la DB):
```
knowledge/
  identity.md
  rules.md
  fact_checking.md
  profiles/
    periodistico/v1.md
    documental/v1.md
    tecnologia/v1.md
    humor/v1.md
    sarcastico-divertido/v1.md
    cine/v1.md
    musica/v1.md
```
Cada perfil usa front-matter YAML para declarar herencia:
```yaml
---
name: documental
inherits: periodistico
version: 1
---
```
La capa `Infrastructure` implementa `IKnowledgeRepository` que lee/parsea estos archivos. La base de datos solo guarda un índice (ruta + versión + hash) para trazabilidad, no el contenido.

## Consecuencias
- Editar un perfil es editar un `.md` y hacer commit — sin desplegar código.
- Resolver herencia es responsabilidad de dominio (`ProfileResolver`), no del filesystem.

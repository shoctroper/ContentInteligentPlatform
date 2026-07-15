# Análisis — Content Intelligence Platform (MVP)

**Basado en:** PRD v1.0 (Mario Alberto Colli Ek, Julio 2026)
**Decisiones tomadas con el usuario:**
- Proveedor IA inicial: **Claude** (interfaz agnóstica desde el día 1, sin credenciales aún)
- Uso: **personal / un solo usuario** (sin auth ni multi-tenancy en MVP)
- Perfiles MVP: **Periodístico, Documental, Tecnología, Humor, Sarcástico-divertido, Cine, Música**
- Despliegue: **Docker**, host por definir (probable servidor propio)

---

## 1. Contexto

El MVP definido por el PRD (sección 19) es: generar guiones desde texto libre, usando perfiles editoriales versionados en Markdown, devolviendo salida en Markdown + JSON, con historial de generaciones. No incluye aún transcripción automática, RSS, scraping ni multi-usuario — eso queda para V2+.

Al ser un solo usuario, se elimina la complejidad de auth/roles/tenancy del alcance inicial, pero el diseño debe dejar un punto de extensión claro para no reescribir cuando se necesite (principio 20 del PRD: "evolucionar sin reescritura completa").

---

## 2. Historias técnicas (MVP)

**H1 — Generar guion desde texto libre**
Como creador de contenido, quiero ingresar texto libre y seleccionar un perfil editorial, para obtener un guion optimizado en Markdown + JSON.
```
Dado un texto de entrada válido y un perfil seleccionado
Cuando solicito la generación
Entonces el sistema ejecuta el pipeline de 8 etapas y devuelve guion + JSON estructurado
```

**H2 — Gestionar perfiles editoriales versionados**
Como creador de contenido, quiero crear/editar perfiles en archivos Markdown con herencia (ej. Periodístico → Documental), para mantener consistencia narrativa sin tocar código.

**H3 — Consultar historial de generaciones**
Como creador de contenido, quiero ver fecha, perfil, prompt, resultado, tokens y costo de cada generación, para mejorar el sistema con el tiempo.

**H4 — Invocar IA a través de una interfaz intercambiable**
Como sistema, quiero desacoplar el pipeline del proveedor de IA mediante una interfaz común, para poder arrancar con Claude y sumar otros proveedores sin tocar la lógica de negocio.

**H5 — Validar hechos antes de redactar (autoevaluación)**
Como creador de contenido, quiero que el sistema marque confianza e información faltante antes de escribir, para reducir alucinaciones (sin fact-checking externo en el MVP).

---

## 3. Event Storming (eventos de dominio)

| Evento | Disparado por | Efecto downstream |
|---|---|---|
| `SourceTextSubmitted` | H1 | Normalización → `NewsItemCreated` |
| `NewsItemCreated` | Normalización | Dispara Pipeline IA |
| `ProfileSelected` | H1 | Alimenta Prompt Builder |
| `ProfileCreated` / `ProfileVersioned` | H2 | Nueva versión disponible para Prompt Builder |
| `PromptBuilt` | H1/H4 | Se envía al AI Provider Gateway |
| `AIProviderInvoked` | H4 | Genera `FactsExtracted`, `ScriptDrafted` |
| `FactsValidated` / `ValidationFlagged` | H5 | Bloquea o continúa redacción |
| `ScriptGenerated` | H1 | Persistencia |
| `GenerationPersisted` | H3 | Disponible en historial |

---

## 4. Bounded Contexts

1. **Source Ingestion** — normaliza texto libre → `NewsItem`. (MVP: solo texto; V2 suma RSS/transcripción)
2. **Knowledge Engine** — repositorio de archivos Markdown (identity, rules, perfiles, ejemplos), fuente de verdad versionable.
3. **Profile Management** — herencia y versionado de perfiles (depende de Knowledge Engine).
4. **Prompt Builder** — compone el prompt final combinando base + perfil + fuente (depende de 1, 2, 3).
5. **AI Generation Pipeline** — orquesta las 8 etapas (comprensión → salida).
6. **AI Provider Gateway** — adapter/strategy para Claude (y futuros proveedores).
7. **Output & Memory** — persiste generaciones, tokens, costo, calificación.

Dependencias: `1,2,3 → 4 → 5 → 6`, y `5 → 7`.

---

## 5. Matriz de riesgo

| ID | Área | Riesgo | Prob. | Impacto | Mitigación |
|---|---|---|---|---|---|
| R1 | Calidad/Dato | Alucinaciones o información inventada | Alta | Alto | Etapa de validación + autoevaluación + revisión humana antes de publicar |
| R2 | Técnico | Acoplamiento accidental a Claude si la interfaz no se diseña bien desde el inicio | Media | Alto | Definir `IAIProvider` desde el día 1 aunque solo exista el adapter de Claude |
| R3 | Costo | Sin monitoreo de gasto en tokens/API | Media | Medio | Registrar tokens/costo por generación desde el MVP (ya está en el PRD, sección 15) |
| R4 | Alcance | PRD muy amplio (7 perfiles, roadmap V2-V4) vs. capacidad de una sola persona | Alta | Medio | Recortar MVP real a 2 perfiles funcionando end-to-end; el resto como plantillas |
| R5 | Integración | "Validación de hechos" no está definida técnicamente (¿fuente externa o solo LLM?) | Media | Medio | MVP usa autoevaluación del LLM (campos `Confidence`/`MissingInformation`), sin fuentes externas aún |
| R6 | Infra | Host de despliegue aún no definido | Baja | Bajo | Dockerizar desde el inicio; agnóstico de host |

---

## 6. Estimación (T-shirt, un solo desarrollador)

| Bounded Context | Tamaño | Rango optimista–pesimista |
|---|---|---|
| Knowledge Engine (estructura + parser md) | S | 3–5 días |
| Profile Management (herencia) | M | 5–8 días |
| Source Ingestion (solo texto) | XS | 1–2 días |
| Prompt Builder | M | 5–7 días |
| AI Provider Gateway (interfaz + adapter Claude) | S | 3–4 días |
| AI Generation Pipeline (8 etapas) | L | 8–12 días |
| Output & Memory (historial, costo, tokens) | S | 3–5 días |
| Frontend Angular (input, perfil, resultado, historial) | L | 8–12 días |
| **Total** | | **36–55 días** (≈ 7–11 semanas a tiempo completo) |

Trabajando part-time, calendario realista: **3–4 meses** para un MVP funcional end-to-end.

---

## 7. Siguiente paso

Handoff a `architect`: definir contratos concretos (OpenAPI), modelo de datos (SQLite MVP), estructura de proyecto Clean Architecture/DDD/CQRS en .NET, y el diseño del `IAIProvider` con el primer adapter para Claude.

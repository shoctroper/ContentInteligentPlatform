# ADR-009: Cuarto proveedor de IA (Google Gemini) — y hallazgo de un bug real

**Estado:** Aceptado

## Contexto
El usuario proporcionó una API key de Google Gemini. A diferencia de DeepSeek y OpenRouter, esta validación
**sí produjo contenido generado real**, lo cual expuso un bug genuino en el pipeline, no solo confirmó la
arquitectura.

## Decisión
Se implementó `GeminiAiProvider : IAiProvider` contra la **Interactions API** de Google
(`https://generativelanguage.googleapis.com/v1beta/interactions`), que es la vía recomendada actual — reemplazó
a la clásica `generateContent` API. Autenticación vía header `x-goog-api-key` (no `Authorization: Bearer`, a
diferencia de los otros tres proveedores). Modelo por defecto `gemini-3.5-flash`, `thinking_level: "minimal"`
(el nivel más bajo disponible: `minimal | low | medium | high`) para minimizar costo/latencia en nuestro caso de
uso de JSON estructurado. El costo se calcula sumando `total_output_tokens + total_thought_tokens` (Google
factura el "thinking" como parte del output) contra precios de referencia configurables.

`AiProvider:Active` ahora soporta `"Claude" | "DeepSeek" | "OpenRouter" | "Gemini"`.

## Bug real encontrado y corregido
En la validación end-to-end, la primera respuesta exitosa (200) de Gemini vino envuelta en un bloque de código
Markdown pese a que el prompt exige "responde ÚNICAMENTE con JSON válido":
```
```json
{"title": "...", ...}
```
```
Esto rompía `JsonSerializer.Deserialize` con `JsonException: '`' is an invalid start of a value`. Claude y
DeepSeek no mostraron este comportamiento en las pruebas realizadas, pero no hay garantía de que nunca lo hagan.

**Fix**: se agregó `LlmJsonResponse.StripMarkdownFence()` (`Application/Common/LlmJsonResponse.cs`), aplicado a
ambas respuestas del pipeline (extracción y guion) **para cualquier proveedor**, no solo Gemini — es un problema
de "contrato con el LLM", no específico de un vendor. 4 tests unitarios nuevos del sanitizador + 1 test de
`GenerateScriptCommandHandler` que reproduce el escenario exacto (respuesta envuelta en fence).

## Validación en vivo — resultado final
Tras el fix, se corrieron 3 intentos consecutivos contra la Api real:
1. **Intento 1**: `403 SERVICE_DISABLED` — "Gemini API has not been used in project ... before or it is disabled
   ... wait a few minutes for the action to propagate." Comportamiento típico de Google Cloud al activar una API
   por primera vez (propagación no instantánea).
2. **Intento 2** (unos segundos después): **`201 Created` — guion real generado** sobre una noticia de prueba
   (inauguración de una línea de metro en CDMX), en español, con estructura completa (`hook`, `introduction`,
   `body`, `ending`, `cta`, `hashtags`, `keywords`, `confidence: 1.0`, `missingInformation` identificando
   correctamente los datos que el texto de prueba no incluía). Tokens y costo reales: 522 in / 521 out, $0.0055.
3. **Intento 3**: otro `201 Created` exitoso, confirmando que no fue un evento aislado.

Esta es la **primera generación de contenido real y verificada de punta a punta** en todo el proyecto (los
intentos con DeepSeek y OpenRouter quedaron bloqueados por falta de saldo, sección ADR-007/ADR-008).

## Consecuencias
- Cuatro proveedores de IA intercambiables: Claude, DeepSeek, OpenRouter, Gemini.
- El fix de `LlmJsonResponse` mejora la robustez de **todos** los proveedores, no solo Gemini — reduce el riesgo
  de que un cambio de comportamiento futuro en cualquier LLM rompa el parseo.
- Queda evidencia real (no solo teórica) de que el pipeline completo — Prompt Builder, llamada real a IA,
  extracción, redacción, persistencia — funciona de principio a fin.
- La API key no se guardó en ningún archivo del repositorio.

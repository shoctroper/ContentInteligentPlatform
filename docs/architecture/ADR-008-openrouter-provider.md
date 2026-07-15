# ADR-008: Tercer proveedor de IA (OpenRouter)

**Estado:** Aceptado

## Contexto
El usuario proporcionó una API key de OpenRouter (agregador que enruta a decenas de modelos: OpenAI, Anthropic,
Meta, etc. bajo una sola API) como alternativa a DeepSeek.

## Decisión
Se implementó `OpenRouterAiProvider : IAiProvider` contra `https://openrouter.ai/api/v1/chat/completions`
(API OpenAI-compatible). Modelo por defecto configurable, `openai/gpt-4o-mini` como valor inicial razonable
(económico y ampliamente disponible); se puede apuntar a cualquier modelo del catálogo de OpenRouter cambiando
`AiProvider:OpenRouter:Model`.

**Diferencia clave con Claude/DeepSeek**: no se mantiene una tabla de precios local por modelo. OpenRouter expone
"Usage Accounting" (activo por defecto desde 2026, sin opt-in) que devuelve el costo real en USD directamente en
`usage.cost` de cada respuesta — más confiable que calcularlo a mano, especialmente porque el modelo puede
cambiar entre requests. Si el campo no viene en la respuesta, se usa `0` como fallback y se registra igual
tokens de entrada/salida.

Se agregó `AiProvider:Active = "OpenRouter"` como tercera opción del switch de selección de proveedor
(ADR-004 + ADR-007). Headers opcionales `HTTP-Referer`/`X-Title` para que las llamadas aparezcan identificadas
en el dashboard de OpenRouter (no afectan la funcionalidad, solo analítica del lado de OpenRouter).

## Validación en vivo
Igual que con DeepSeek (ADR-007): se corrió la Api real con la key provista contra el mismo texto de prueba.
Resultado: `402 Payment Required` — *"Insufficient credits. This account never purchased credits."* Confirma que
la key es válida y el request llega correctamente formado (un 401 habría indicado key inválida). La cuenta de
OpenRouter del usuario no tiene créditos cargados todavía.

## Consecuencias
- Tres proveedores de IA ahora intercambiables por una sola línea de configuración: Claude, DeepSeek, OpenRouter
  (y, vía OpenRouter, indirectamente acceso a cualquier modelo de su catálogo sin escribir un adapter nuevo).
- Persiste el mismo hallazgo operativo que con DeepSeek: **ninguna de las dos cuentas de IA que el usuario ha
  probado hasta ahora tiene saldo/créditos cargados**. Antes de poder ver contenido generado real, el usuario
  necesita cargar saldo en al menos una de las tres cuentas (Claude, DeepSeek u OpenRouter).
- La API key no se guardó en ningún archivo del repositorio; se usó solo como variable de entorno transitoria
  para esta validación.

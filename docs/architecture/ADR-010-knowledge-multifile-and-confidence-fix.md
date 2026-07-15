# ADR-010: Conocimiento base multi-archivo + corrección de pérdida de confidence/missingInformation entre etapas

**Estado:** Aceptado

## Contexto
El usuario proporcionó el contenido completo de 5 documentos editoriales (Manifiesto, Identidad, Pensamiento,
Storytelling, Voz) para reemplazar/ampliar el `identity.md` original, que era un placeholder. `GenerateScriptCommandHandler`
solo leía `identity.md` como conocimiento base; había que combinar los 6 archivos base (`manifesto.md`,
`identity.md`, `rules.md`, `thinking.md`, `storytelling.md`, `voice.md`).

Durante la validación en vivo de este cambio (usando la API key de Gemini y un caso real: un texto sobre el
episodio 126 de "Loret en Latinus"), se hizo una demo manual del pipeline de dos etapas y se detectó un bug real:

1. **Etapa de extracción** detectó correctamente un vacío de información legítimo (el texto describe a Ernestina
   Godoy como fiscal de la CDMX, pero la captura de "El Mayo" Zambada ocurrió en una fecha en la que ese dato es
   cuestionable) y reportó `confidence: 0.9` con un `missingInformation` explícito.
2. **Etapa de redacción**, al recibir solo los `facts` y el `summary` de la extracción (nunca el `missingInformation`
   ni el `confidence` de esa etapa), generó un guion con `confidence: 1.0` y `missingInformation: null` —
   el vacío detectado se perdió por completo en el resultado final persistido, que es el único que ve el usuario.

Esto viola directamente un principio explícito del Manifiesto/Reglas del proyecto: declarar información faltante
en vez de ocultarla o resolverla implícitamente.

## Decisión
1. **Conocimiento base combinado**: `GenerateScriptCommandHandler` ahora lee los 6 archivos base en el orden
   manifesto → identity → rules → thinking → storytelling → voice (por qué → quién → límites → cómo pensar →
   cómo narrar → cómo suena) y los concatena con separador `\n\n---\n\n`. Archivos ausentes o vacíos se omiten
   sin error (permite migrar gradualmente).
2. **El prompt de redacción ahora incluye explícitamente el `missingInformation` de la etapa de extracción**,
   instruyendo al modelo a no ignorarlo y a no reportar una confianza mayor a la de extracción sin justificar
   por qué el vacío ya no aplica.
3. **Salvaguarda a nivel de código (no solo de prompt)**: el handler ya no persiste `script.Confidence` /
   `script.MissingInformation` directamente. Calcula:
   - `finalConfidence = Math.Min(extraction.Confidence, script.Confidence)` — la confianza final nunca puede
     ser mayor que la etapa más insegura.
   - `finalMissingInformation = CombineMissingInformation(...)` — si ambas etapas reportan algo distinto, se
     concatenan; si solo una lo reporta, se conserva; si el guion ya lo reformuló, se usa esa versión.

   Este es un ejemplo concreto de por qué el proyecto no puede confiar en instrucciones de prompt como único
   mecanismo de cumplimiento de reglas de negocio críticas (sección de riesgos del PRD, R1: alucinaciones/exceso
   de confianza) — se necesita una verificación programática independiente del modelo.

## Consecuencias
- El campo `confidence` mostrado al usuario ahora es más conservador (mínimo de ambas etapas) — puede bajar
  ligeramente el promedio percibido de confianza, mensaje esperado y deseado.
- `missingInformation` puede volverse más largo/compuesto cuando ambas etapas detectan cosas distintas.
- Se agregó `GenerateScriptCommandHandlerTests.Handle_ConVacioDeInformacionSoloEnExtraccion_DeberiaConservarloEnElResultadoFinal`
  como regresión, replicando el caso real observado (extracción con vacío + guion que lo omite).
- Pendiente de evaluar en próximas iteraciones: exponer en el frontend cuándo el `missingInformation` final
  proviene de una combinación de dos etapas (actualmente es una sola cadena de texto sin metadata de origen).

# ADR-007: Segundo proveedor de IA (DeepSeek) y selección por configuración

**Estado:** Aceptado

## Contexto
ADR-004 definió la interfaz `IAiProvider` como puerto intercambiable, pero solo existía un adapter (Claude) y la
selección del proveedor activo estaba hardcodeada en el DI de `Program.cs`. El usuario proporcionó una API key
de DeepSeek, lo cual es la primera oportunidad real de validar que la arquitectura de proveedor intercambiable
funciona en la práctica, no solo en el diseño.

## Decisión
1. Se implementó `DeepSeekAiProvider : IAiProvider`, adapter HTTP contra `https://api.deepseek.com/chat/completions`
   (API OpenAI-compatible). Modelo por defecto `deepseek-v4-flash` (los nombres `deepseek-chat`/`deepseek-reasoner`
   se deprecan el 2026/07/24 según la documentación oficial, así que se usa el nombre nuevo directamente).
2. Se deshabilita el "thinking mode" de DeepSeek por defecto (`thinking: {type: "disabled"}`): para nuestro caso de
   uso (devolver JSON estructurado limpio) no aporta valor y sí agrega costo/latencia con contenido de
   razonamiento (`reasoning_content`) que tendríamos que descartar de todas formas.
3. Cálculo de costo distingue tokens de input cache-hit vs cache-miss (precios distintos en DeepSeek); si el
   response no trae ese detalle, se asume el escenario conservador (100% cache-miss).
4. **Se completó la promesa de ADR-004**: el proveedor activo ahora se elige por configuración
   (`AiProvider:Active` = `"Claude"` | `"DeepSeek"`), resuelto en el contenedor de DI vía `IHttpClientFactory`
   con clientes HTTP nombrados. Antes esto estaba hardcodeado a Claude.

## Validación en vivo
Se corrió la Api real (no mocks) en el sandbox de desarrollo con `AiProvider:Active=DeepSeek` y la API key
proporcionada, contra un texto de prueba real. Resultado:
- Conexión, autenticación y formato del request: **correctos** — DeepSeek respondió `402 Insufficient Balance`,
  no `401 Unauthorized` ni un error de formato. Un 401 habría significado key inválida; el 402 confirma que la
  key es válida pero la cuenta no tiene saldo cargado.
- El pipeline completo (Prompt Builder → HTTP real → manejo de error → `ProblemDetails`) funcionó de punta a
  punta, incluido el mapeo del error de DeepSeek a una respuesta 500 limpia para el cliente.
- **No se pudo confirmar la calidad del contenido generado** porque la cuenta de DeepSeek no tiene saldo. Para
  validar esto falta que el usuario cargue saldo en https://platform.deepseek.com/ o provea otra key con saldo.

## Consecuencias
- La arquitectura de "IA intercambiable" (principio del PRD, sección 20) queda demostrada, no solo documentada.
- Agregar un tercer proveedor (OpenAI, Gemini, Ollama) es ahora: una clase que implemente `IAiProvider`, un
  registro de opciones + `HttpClient` nombrado, y un caso más en el `switch` de selección — sin tocar
  Domain/Application.
- La API key de DeepSeek proporcionada por el usuario **no se guardó en ningún archivo del repositorio**; se usó
  solo como variable de entorno transitoria en el sandbox para esta validación. Se recomienda rotarla/protegerla
  igual que cualquier credencial compartida por chat.

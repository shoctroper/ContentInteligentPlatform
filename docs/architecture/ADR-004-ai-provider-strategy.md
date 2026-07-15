# ADR-004: Proveedor de IA intercambiable

**Estado:** Aceptado

## Contexto
PRD principio rector: "La IA es intercambiable" (sección 20). Se arranca con Claude, pero la interfaz debe soportar OpenAI, Gemini, Ollama, etc. sin cambiar el pipeline (R2 del análisis).

## Decisión
Patrón Strategy/Adapter. `Application` define el puerto:
```csharp
public interface IAiProvider
{
    string Name { get; }
    Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken ct);
}
```
`Infrastructure` implementa `ClaudeAiProvider` (HTTP a la API de Anthropic vía `HttpClient` nombrado, API key desde configuración/user-secrets — **no hardcodeada**, pendiente de que el usuario provea la key). El pipeline (`Application`) depende únicamente de `IAiProvider`, resuelto por DI; el proveedor activo se selecciona por configuración (`AiProvider:Active = "Claude"`).

## Consecuencias
- Sumar OpenAI/Ollama después = una clase nueva + un registro en DI, cero cambios en Domain/Application.
- Sin API key configurada, el sistema debe fallar de forma explícita y clara (no simular resultados).

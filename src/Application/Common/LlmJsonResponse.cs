namespace ContentIntelligencePlatform.Application.Common;

/// <summary>
/// Algunos proveedores de IA (ej. Gemini) envuelven su respuesta en un bloque de código Markdown
/// (```json ... ``` o ``` ... ```) a pesar de que el prompt pide "solo JSON". Esto se detectó en
/// pruebas reales end-to-end (ADR-009) — Claude y DeepSeek no lo hicieron en las pruebas realizadas,
/// pero Gemini sí. Se sanea antes de deserializar, sin importar el proveedor.
/// </summary>
public static class LlmJsonResponse
{
    public static string StripMarkdownFence(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
            return trimmed;

        var firstNewline = trimmed.IndexOf('\n');
        if (firstNewline < 0)
            return trimmed;

        var withoutOpeningFence = trimmed[(firstNewline + 1)..];

        var closingFenceIndex = withoutOpeningFence.LastIndexOf("```", StringComparison.Ordinal);
        if (closingFenceIndex >= 0)
            withoutOpeningFence = withoutOpeningFence[..closingFenceIndex];

        return withoutOpeningFence.Trim();
    }
}

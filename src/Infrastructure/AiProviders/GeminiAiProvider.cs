using System.Text;
using System.Text.Json;
using ContentIntelligencePlatform.Application.Abstractions;

namespace ContentIntelligencePlatform.Infrastructure.AiProviders;

public class GeminiAiProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-3.5-flash";
    /// <summary>minimal | low | medium | high. "minimal" para nuestro caso (JSON estructurado, sin necesidad de razonamiento largo).</summary>
    public string ThinkingLevel { get; set; } = "minimal";
    // Precios de referencia (ai.google.dev/gemini-api/docs/pricing, jul-2026) por millón de tokens, tier Standard.
    public decimal InputCostPerMillionTokens { get; set; } = 1.50m;
    public decimal OutputCostPerMillionTokens { get; set; } = 9.00m; // incluye "thinking tokens" según pricing de Google.
}

/// <summary>
/// Adapter de IAiProvider para Google Gemini vía la Interactions API (ADR-004, ADR-009).
/// Nota: Google reemplazó la clásica generateContent API por la Interactions API (v1beta/interactions)
/// como vía recomendada en 2026; esta implementación usa esa API nueva, no la legacy.
/// Application NUNCA referencia esta clase directamente, solo la interfaz IAiProvider.
/// </summary>
public class GeminiAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly GeminiAiProviderOptions _options;

    public string Name => "Gemini";

    public GeminiAiProvider(HttpClient httpClient, GeminiAiProviderOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException(
                "No hay API key de Gemini configurada (AiProvider:Gemini:ApiKey). " +
                "Configúrala con 'dotnet user-secrets set' antes de generar contenido real.");

        var payload = new
        {
            model = _options.Model,
            input = request.UserPrompt,
            system_instruction = request.SystemPrompt,
            generation_config = new { thinking_level = _options.ThinkingLevel }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://generativelanguage.googleapis.com/v1beta/interactions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("x-goog-api-key", _options.ApiKey);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Gemini API devolvió {(int)response.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var text = ExtractText(root);
        var modelUsed = root.TryGetProperty("model", out var modelProp) ? modelProp.GetString() ?? _options.Model : _options.Model;

        var usage = root.GetProperty("usage");
        var inputTokens = usage.GetProperty("total_input_tokens").GetInt32();
        var outputTokens = usage.GetProperty("total_output_tokens").GetInt32();
        // El "thinking" se factura como parte del output (ver pricing de Google); lo sumamos para reflejar el costo real.
        var thoughtTokens = usage.TryGetProperty("total_thought_tokens", out var thoughtProp) ? thoughtProp.GetInt32() : 0;
        var billableOutputTokens = outputTokens + thoughtTokens;

        var cost = inputTokens / 1_000_000m * _options.InputCostPerMillionTokens
                 + billableOutputTokens / 1_000_000m * _options.OutputCostPerMillionTokens;

        return new AiCompletionResult(text, inputTokens, billableOutputTokens, cost, modelUsed);
    }

    private static string ExtractText(JsonElement root)
    {
        if (!root.TryGetProperty("steps", out var steps))
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var step in steps.EnumerateArray())
        {
            if (!step.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "model_output")
                continue;
            if (!step.TryGetProperty("content", out var contentBlocks))
                continue;

            foreach (var block in contentBlocks.EnumerateArray())
            {
                if (block.TryGetProperty("type", out var blockType) && blockType.GetString() == "text"
                    && block.TryGetProperty("text", out var textProp))
                {
                    sb.Append(textProp.GetString());
                }
            }
        }

        return sb.ToString();
    }
}

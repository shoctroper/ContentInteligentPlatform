using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ContentIntelligencePlatform.Application.Abstractions;

namespace ContentIntelligencePlatform.Infrastructure.AiProviders;

public class OpenRouterAiProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "openai/gpt-4o-mini";
    /// <summary>Opcional: aparece en el dashboard de OpenRouter (header HTTP-Referer).</summary>
    public string? SiteUrl { get; set; }
    /// <summary>Opcional: aparece en el dashboard de OpenRouter (header X-Title).</summary>
    public string? SiteName { get; set; }
}

/// <summary>
/// Adapter de IAiProvider para OpenRouter (agregador multi-modelo, API OpenAI-compatible), ver ADR-004 y ADR-008.
/// A diferencia de Claude/DeepSeek, el costo no se calcula con una tabla de precios local: OpenRouter devuelve
/// el costo real en USD directamente en `usage.cost` en cada respuesta (feature "Usage Accounting", siempre
/// activo desde 2026, sin necesidad de opt-in), lo cual es más confiable dado que el modelo puede cambiar
/// por request.
/// </summary>
public class OpenRouterAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly OpenRouterAiProviderOptions _options;

    public string Name => "OpenRouter";

    public OpenRouterAiProvider(HttpClient httpClient, OpenRouterAiProviderOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException(
                "No hay API key de OpenRouter configurada (AiProvider:OpenRouter:ApiKey). " +
                "Configúrala con 'dotnet user-secrets set' antes de generar contenido real.");

        var payload = new
        {
            model = _options.Model,
            max_tokens = request.MaxTokens,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(_options.SiteUrl))
            httpRequest.Headers.Add("HTTP-Referer", _options.SiteUrl);
        if (!string.IsNullOrWhiteSpace(_options.SiteName))
            httpRequest.Headers.Add("X-Title", _options.SiteName);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenRouter API devolvió {(int)response.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            throw new InvalidOperationException($"OpenRouter no devolvió 'choices' en la respuesta: {body}");

        var text = choices[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        var modelUsed = root.TryGetProperty("model", out var modelProp) ? modelProp.GetString() ?? _options.Model : _options.Model;

        var usage = root.GetProperty("usage");
        var inputTokens = usage.GetProperty("prompt_tokens").GetInt32();
        var outputTokens = usage.GetProperty("completion_tokens").GetInt32();
        var cost = usage.TryGetProperty("cost", out var costProp) ? costProp.GetDecimal() : 0m;

        return new AiCompletionResult(text, inputTokens, outputTokens, cost, modelUsed);
    }
}

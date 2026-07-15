using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ContentIntelligencePlatform.Application.Abstractions;

namespace ContentIntelligencePlatform.Infrastructure.AiProviders;

public class DeepSeekAiProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "deepseek-v4-flash";
    public bool ThinkingEnabled { get; set; } = false; // deshabilitado por defecto: queremos JSON limpio en `content`, no CoT.
    // Precios de referencia (docs.deepseek.com/quick_start/pricing, jul-2026) por millón de tokens.
    public decimal CacheHitInputCostPerMillionTokens { get; set; } = 0.0028m;
    public decimal CacheMissInputCostPerMillionTokens { get; set; } = 0.14m;
    public decimal OutputCostPerMillionTokens { get; set; } = 0.28m;
}

/// <summary>
/// Adapter de IAiProvider para DeepSeek (API OpenAI-compatible), ver ADR-004 y ADR-007.
/// Application NUNCA referencia esta clase directamente, solo la interfaz IAiProvider.
/// </summary>
public class DeepSeekAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly DeepSeekAiProviderOptions _options;

    public string Name => "DeepSeek";

    public DeepSeekAiProvider(HttpClient httpClient, DeepSeekAiProviderOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException(
                "No hay API key de DeepSeek configurada (AiProvider:DeepSeek:ApiKey). " +
                "Configúrala con 'dotnet user-secrets set' antes de generar contenido real.");

        var payload = new
        {
            model = _options.Model,
            max_tokens = request.MaxTokens,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt }
            },
            thinking = new { type = _options.ThinkingEnabled ? "enabled" : "disabled" }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.deepseek.com/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"DeepSeek API devolvió {(int)response.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var text = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;

        var usage = root.GetProperty("usage");
        var inputTokens = usage.GetProperty("prompt_tokens").GetInt32();
        var outputTokens = usage.GetProperty("completion_tokens").GetInt32();

        // DeepSeek factura input cacheado y no-cacheado a precios distintos; si el response no trae el detalle
        // (prompt_cache_hit_tokens / prompt_cache_miss_tokens), asumimos el escenario conservador: 100% cache-miss.
        var cacheHitTokens = usage.TryGetProperty("prompt_cache_hit_tokens", out var hitProp) ? hitProp.GetInt32() : 0;
        var cacheMissTokens = usage.TryGetProperty("prompt_cache_miss_tokens", out var missProp)
            ? missProp.GetInt32()
            : inputTokens - cacheHitTokens;

        var cost = cacheHitTokens / 1_000_000m * _options.CacheHitInputCostPerMillionTokens
                 + cacheMissTokens / 1_000_000m * _options.CacheMissInputCostPerMillionTokens
                 + outputTokens / 1_000_000m * _options.OutputCostPerMillionTokens;

        return new AiCompletionResult(text, inputTokens, outputTokens, cost, _options.Model);
    }
}

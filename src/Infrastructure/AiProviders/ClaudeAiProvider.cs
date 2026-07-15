using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ContentIntelligencePlatform.Application.Abstractions;

namespace ContentIntelligencePlatform.Infrastructure.AiProviders;

public class ClaudeAiProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-4-5-20250929";
    // Precios de referencia por millón de tokens; deben actualizarse al configurar la key real.
    public decimal InputCostPerMillionTokens { get; set; } = 3.0m;
    public decimal OutputCostPerMillionTokens { get; set; } = 15.0m;
}

/// <summary>
/// Adapter concreto de IAiProvider para Anthropic Claude (ADR-004).
/// Application NUNCA referencia esta clase directamente, solo la interfaz.
/// </summary>
public class ClaudeAiProvider : IAiProvider
{
    private const string ApiVersion = "2023-06-01";
    private readonly HttpClient _httpClient;
    private readonly ClaudeAiProviderOptions _options;

    public string Name => "Claude";

    public ClaudeAiProvider(HttpClient httpClient, ClaudeAiProviderOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException(
                "No hay API key de Claude configurada (AiProvider:Claude:ApiKey). " +
                "Configúrala con 'dotnet user-secrets set' antes de generar contenido real.");

        var payload = new
        {
            model = _options.Model,
            max_tokens = request.MaxTokens,
            system = request.SystemPrompt,
            messages = new[] { new { role = "user", content = request.UserPrompt } }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", ApiVersion);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Claude API devolvió {(int)response.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var text = root.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;
        var inputTokens = root.GetProperty("usage").GetProperty("input_tokens").GetInt32();
        var outputTokens = root.GetProperty("usage").GetProperty("output_tokens").GetInt32();

        var cost = inputTokens / 1_000_000m * _options.InputCostPerMillionTokens
                 + outputTokens / 1_000_000m * _options.OutputCostPerMillionTokens;

        return new AiCompletionResult(text, inputTokens, outputTokens, cost, _options.Model);
    }
}

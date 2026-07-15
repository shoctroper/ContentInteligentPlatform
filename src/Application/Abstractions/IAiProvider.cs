namespace ContentIntelligencePlatform.Application.Abstractions;

public record AiCompletionRequest(string SystemPrompt, string UserPrompt, int MaxTokens = 4096);

public record AiCompletionResult(string Text, int TokensInput, int TokensOutput, decimal CostUsd, string Model);

/// <summary>
/// Puerto de dominio para cualquier proveedor de IA (ADR-004). Application nunca conoce Claude/OpenAI directamente.
/// </summary>
public interface IAiProvider
{
    string Name { get; }
    Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken);
}

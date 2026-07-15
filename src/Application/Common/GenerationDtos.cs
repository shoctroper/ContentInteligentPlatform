namespace ContentIntelligencePlatform.Application.Common;

public record ExtractionResultDto(string Title, string Summary, List<string> Facts, decimal Confidence, string? MissingInformation);

public record ScriptResultDto(
    string Title, string Hook, string Introduction, string Body, string Ending, string Cta,
    List<string> Hashtags, List<string> Keywords, string Category, int EstimatedDurationSeconds,
    decimal Confidence, string? MissingInformation, List<string> Sources);

public record GenerationDetailDto(
    Guid Id, string ProfileSlug, string ProviderName, string ResultMarkdown, string ResultJson,
    decimal Confidence, string? MissingInformation, int TokensInput, int TokensOutput, decimal CostUsd,
    int? Rating, DateTimeOffset CreatedAt);

public record GenerationSummaryDto(Guid Id, string ProfileSlug, DateTimeOffset CreatedAt, int? Rating);

public record ProfileDto(Guid Id, string Slug, string Name, string? ParentSlug, int Version);

namespace ContentIntelligencePlatform.Application.Abstractions;

public record KnowledgeProfileDocument(string Slug, string Name, string? ParentSlug, int Version, string FilePath, string ContentMarkdown, string ContentHash);

/// <summary>
/// Puerto sobre knowledge/ (ADR-003). El contenido editorial vive en archivos Markdown versionados con Git.
/// </summary>
public interface IKnowledgeRepository
{
    Task<KnowledgeProfileDocument?> FindProfileAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeProfileDocument>> GetAllProfilesAsync(CancellationToken cancellationToken);
    Task<string> ReadBaseKnowledgeAsync(string fileName, CancellationToken cancellationToken);
}

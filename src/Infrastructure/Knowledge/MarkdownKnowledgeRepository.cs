using System.Security.Cryptography;
using System.Text;
using ContentIntelligencePlatform.Application.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ContentIntelligencePlatform.Infrastructure.Knowledge;

/// <summary>
/// Lee knowledge/ del filesystem (ADR-003). El contenido editorial nunca vive en la base de datos.
/// </summary>
public class MarkdownKnowledgeRepository : IKnowledgeRepository
{
    private readonly string _knowledgeRootPath;
    private readonly IDeserializer _yamlDeserializer;

    public MarkdownKnowledgeRepository(string knowledgeRootPath)
    {
        _knowledgeRootPath = knowledgeRootPath;
        _yamlDeserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
    }

    public async Task<KnowledgeProfileDocument?> FindProfileAsync(string slug, CancellationToken cancellationToken)
    {
        var all = await GetAllProfilesAsync(cancellationToken);
        return all.FirstOrDefault(p => p.Slug == slug.Trim().ToLowerInvariant());
    }

    public async Task<IReadOnlyList<KnowledgeProfileDocument>> GetAllProfilesAsync(CancellationToken cancellationToken)
    {
        var profilesDir = Path.Combine(_knowledgeRootPath, "profiles");
        if (!Directory.Exists(profilesDir))
            return Array.Empty<KnowledgeProfileDocument>();

        var results = new List<KnowledgeProfileDocument>();
        foreach (var file in Directory.EnumerateFiles(profilesDir, "*.md", SearchOption.AllDirectories))
        {
            var raw = await File.ReadAllTextAsync(file, cancellationToken);
            var (frontMatter, body) = SplitFrontMatter(raw);
            var meta = _yamlDeserializer.Deserialize<ProfileFrontMatter>(frontMatter);

            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..16];

            results.Add(new KnowledgeProfileDocument(
                meta.Slug.ToLowerInvariant(), meta.Name, meta.Inherits, meta.Version, file, body, hash));
        }

        return results;
    }

    public async Task<string> ReadBaseKnowledgeAsync(string fileName, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_knowledgeRootPath, fileName);
        if (!File.Exists(path))
            return string.Empty;

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    private static (string FrontMatter, string Body) SplitFrontMatter(string raw)
    {
        const string delimiter = "---";
        if (!raw.TrimStart().StartsWith(delimiter))
            throw new InvalidOperationException("El archivo de perfil no tiene front-matter YAML.");

        var parts = raw.TrimStart().Split(delimiter, 3, StringSplitOptions.None);
        if (parts.Length < 3)
            throw new InvalidOperationException("Front-matter YAML mal formado.");

        return (parts[1], parts[2].Trim());
    }

    private class ProfileFrontMatter
    {
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Inherits { get; set; }
        public int Version { get; set; } = 1;
    }
}

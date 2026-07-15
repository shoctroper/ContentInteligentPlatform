using ContentIntelligencePlatform.Domain.Common;

namespace ContentIntelligencePlatform.Domain.Profiles;

public class Profile : Entity
{
    public string Slug { get; private set; }
    public string Name { get; private set; }
    public string? ParentSlug { get; private set; }
    public int Version { get; private set; }
    public string FilePath { get; private set; }
    public string ContentHash { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Profile(Guid id, string slug, string name, string? parentSlug, int version, string filePath, string contentHash, DateTimeOffset createdAt)
        : base(id)
    {
        Slug = slug;
        Name = name;
        ParentSlug = parentSlug;
        Version = version;
        FilePath = filePath;
        ContentHash = contentHash;
        CreatedAt = createdAt;
    }

    public static Result<Profile> Create(string slug, string name, string? parentSlug, int version, string filePath, string contentHash, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Result.Failure<Profile>("El slug del perfil es obligatorio.");
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Profile>("El nombre del perfil es obligatorio.");
        if (version < 1)
            return Result.Failure<Profile>("La versión del perfil debe ser >= 1.");
        if (string.Equals(slug, parentSlug, StringComparison.OrdinalIgnoreCase))
            return Result.Failure<Profile>("Un perfil no puede heredar de sí mismo.");

        return Result.Success(new Profile(Guid.NewGuid(), slug.Trim().ToLowerInvariant(), name.Trim(), parentSlug?.Trim().ToLowerInvariant(), version, filePath, contentHash, createdAt));
    }
}

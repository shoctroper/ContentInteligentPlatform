using ContentIntelligencePlatform.Domain.Common;

namespace ContentIntelligencePlatform.Domain.Sources;

public class Source : Entity
{
    public SourceType Type { get; private set; }
    public string RawContent { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }

    private Source(Guid id, SourceType type, string rawContent, DateTimeOffset submittedAt) : base(id)
    {
        Type = type;
        RawContent = rawContent;
        SubmittedAt = submittedAt;
    }

    public static Result<Source> Create(SourceType type, string rawContent, DateTimeOffset submittedAt)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
            return Result.Failure<Source>("El contenido de la fuente no puede estar vacío.");

        return Result.Success(new Source(Guid.NewGuid(), type, rawContent, submittedAt));
    }
}

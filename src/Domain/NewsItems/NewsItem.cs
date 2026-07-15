using ContentIntelligencePlatform.Domain.Common;

namespace ContentIntelligencePlatform.Domain.NewsItems;

public class NewsItem : Entity
{
    public Guid SourceId { get; private set; }
    public string Title { get; private set; }
    public string Summary { get; private set; }
    public string FactsJson { get; private set; }
    public Confidence Confidence { get; private set; }
    public string? MissingInformation { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private NewsItem(Guid id, Guid sourceId, string title, string summary, string factsJson, Confidence confidence, string? missingInformation, DateTimeOffset createdAt)
        : base(id)
    {
        SourceId = sourceId;
        Title = title;
        Summary = summary;
        FactsJson = factsJson;
        Confidence = confidence;
        MissingInformation = missingInformation;
        CreatedAt = createdAt;
    }

    public static Result<NewsItem> Create(Guid sourceId, string title, string summary, string factsJson, decimal confidence, string? missingInformation, DateTimeOffset createdAt)
    {
        if (sourceId == Guid.Empty)
            return Result.Failure<NewsItem>("SourceId es obligatorio.");
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<NewsItem>("El título es obligatorio.");

        var confidenceResult = Confidence.Create(confidence);
        if (confidenceResult.IsFailure)
            return Result.Failure<NewsItem>(confidenceResult.Error!);

        return Result.Success(new NewsItem(Guid.NewGuid(), sourceId, title, summary, factsJson, confidenceResult.Value, missingInformation, createdAt));
    }
}

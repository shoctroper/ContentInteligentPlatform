using ContentIntelligencePlatform.Domain.Common;

namespace ContentIntelligencePlatform.Domain.Generations;

public class Generation : Entity
{
    public Guid NewsItemId { get; private set; }
    public Guid ProfileId { get; private set; }
    public string ProviderName { get; private set; }
    public string Model { get; private set; }
    public string PromptText { get; private set; }
    public string ResultMarkdown { get; private set; }
    public string ResultJson { get; private set; }
    public int TokensInput { get; private set; }
    public int TokensOutput { get; private set; }
    public decimal CostUsd { get; private set; }
    public int DurationMs { get; private set; }
    public Rating? Rating { get; private set; }
    public string? Comments { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Generation(
        Guid id, Guid newsItemId, Guid profileId, string providerName, string model,
        string promptText, string resultMarkdown, string resultJson,
        int tokensInput, int tokensOutput, decimal costUsd, int durationMs, DateTimeOffset createdAt)
        : base(id)
    {
        NewsItemId = newsItemId;
        ProfileId = profileId;
        ProviderName = providerName;
        Model = model;
        PromptText = promptText;
        ResultMarkdown = resultMarkdown;
        ResultJson = resultJson;
        TokensInput = tokensInput;
        TokensOutput = tokensOutput;
        CostUsd = costUsd;
        DurationMs = durationMs;
        CreatedAt = createdAt;
    }

    public static Result<Generation> Create(
        Guid newsItemId, Guid profileId, string providerName, string model,
        string promptText, string resultMarkdown, string resultJson,
        int tokensInput, int tokensOutput, decimal costUsd, int durationMs, DateTimeOffset createdAt)
    {
        if (newsItemId == Guid.Empty)
            return Result.Failure<Generation>("NewsItemId es obligatorio.");
        if (profileId == Guid.Empty)
            return Result.Failure<Generation>("ProfileId es obligatorio.");
        if (string.IsNullOrWhiteSpace(providerName))
            return Result.Failure<Generation>("ProviderName es obligatorio.");
        if (tokensInput < 0 || tokensOutput < 0)
            return Result.Failure<Generation>("Los tokens no pueden ser negativos.");
        if (costUsd < 0)
            return Result.Failure<Generation>("El costo no puede ser negativo.");

        return Result.Success(new Generation(
            Guid.NewGuid(), newsItemId, profileId, providerName, model,
            promptText, resultMarkdown, resultJson, tokensInput, tokensOutput, costUsd, durationMs, createdAt));
    }

    public Result Rate(int rating, string? comments)
    {
        var ratingResult = Domain.Generations.Rating.Create(rating);
        if (ratingResult.IsFailure)
            return Result.Failure(ratingResult.Error!);

        Rating = ratingResult.Value;
        Comments = comments;
        return Result.Success();
    }
}

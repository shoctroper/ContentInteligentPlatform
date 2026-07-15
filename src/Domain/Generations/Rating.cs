using ContentIntelligencePlatform.Domain.Common;

namespace ContentIntelligencePlatform.Domain.Generations;

public readonly record struct Rating
{
    public int Value { get; }

    private Rating(int value) => Value = value;

    public static Result<Rating> Create(int value)
    {
        if (value is < 1 or > 5)
            return Result.Failure<Rating>("El rating debe estar entre 1 y 5.");

        return Result.Success(new Rating(value));
    }
}

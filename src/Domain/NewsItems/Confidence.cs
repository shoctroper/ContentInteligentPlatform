using ContentIntelligencePlatform.Domain.Common;

namespace ContentIntelligencePlatform.Domain.NewsItems;

public readonly record struct Confidence
{
    public decimal Value { get; }

    private Confidence(decimal value) => Value = value;

    public static Result<Confidence> Create(decimal value)
    {
        if (value < 0m || value > 1m)
            return Result.Failure<Confidence>("Confidence debe estar entre 0.0 y 1.0.");

        return Result.Success(new Confidence(value));
    }

    public override string ToString() => Value.ToString("0.00");
}

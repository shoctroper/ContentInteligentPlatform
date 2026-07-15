using ContentIntelligencePlatform.Domain.Generations;
using FluentAssertions;
using Xunit;

namespace ContentIntelligencePlatform.Domain.Tests;

public class RatingTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Create_ConValorValido_DeberiaSerExitoso(int value)
    {
        var result = Rating.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Create_ConValorFueraDeRango_DeberiaFallar(int value)
    {
        var result = Rating.Create(value);

        result.IsFailure.Should().BeTrue();
    }
}

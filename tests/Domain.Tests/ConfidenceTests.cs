using ContentIntelligencePlatform.Domain.NewsItems;
using FluentAssertions;
using Xunit;

namespace ContentIntelligencePlatform.Domain.Tests;

public class ConfidenceTests
{
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Create_ConValorValido_DeberiaSerExitoso(decimal value)
    {
        var result = Confidence.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Create_ConValorFueraDeRango_DeberiaFallar(decimal value)
    {
        var result = Confidence.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("0.0 y 1.0");
    }
}

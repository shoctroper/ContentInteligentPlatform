using ContentIntelligencePlatform.Application.Generations.Commands;
using FluentAssertions;
using Xunit;

namespace ContentIntelligencePlatform.Application.Tests;

public class GenerateScriptCommandValidatorTests
{
    private readonly GenerateScriptCommandValidator _validator = new();

    [Fact]
    public void Validate_ConDatosValidos_NoDeberiaTenerErrores()
    {
        var result = _validator.Validate(new GenerateScriptCommand("texto", "periodistico", "TikTok"));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "periodistico", "TikTok")]
    [InlineData("texto", "", "TikTok")]
    [InlineData("texto", "periodistico", "")]
    public void Validate_ConCampoVacio_DeberiaTenerErrores(string sourceText, string profileSlug, string outputFormat)
    {
        var result = _validator.Validate(new GenerateScriptCommand(sourceText, profileSlug, outputFormat));
        result.IsValid.Should().BeFalse();
    }
}

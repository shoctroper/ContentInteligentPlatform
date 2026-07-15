using ContentIntelligencePlatform.Domain.Profiles;
using FluentAssertions;
using Xunit;

namespace ContentIntelligencePlatform.Domain.Tests;

public class ProfileTests
{
    [Fact]
    public void Create_ConDatosValidos_DeberiaSerExitoso()
    {
        var result = Profile.Create("periodistico", "Periodístico", null, 1, "profiles/periodistico.md", "hash", DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("periodistico");
    }

    [Fact]
    public void Create_ConSlugVacio_DeberiaFallar()
    {
        var result = Profile.Create("", "Nombre", null, 1, "path.md", "hash", DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_HeredandoDeSiMismo_DeberiaFallar()
    {
        var result = Profile.Create("periodistico", "Periodístico", "periodistico", 1, "path.md", "hash", DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no puede heredar de sí mismo");
    }

    [Fact]
    public void Create_ConVersionMenorAUno_DeberiaFallar()
    {
        var result = Profile.Create("periodistico", "Periodístico", null, 0, "path.md", "hash", DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
    }
}

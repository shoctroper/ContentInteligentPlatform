using ContentIntelligencePlatform.Domain.Profiles;
using FluentAssertions;
using Xunit;

namespace ContentIntelligencePlatform.Domain.Tests;

public class ProfileInheritanceResolverTests
{
    private readonly ProfileInheritanceResolver _resolver = new();

    [Fact]
    public void ResolveChain_ConHerenciaSimple_DeberiaRetornarRaizPrimero()
    {
        var periodistico = Profile.Create("periodistico", "Periodístico", null, 1, "p.md", "h", DateTimeOffset.UtcNow).Value;
        var documental = Profile.Create("documental", "Documental", "periodistico", 1, "d.md", "h", DateTimeOffset.UtcNow).Value;

        var bySlug = new Dictionary<string, Profile> { [periodistico.Slug] = periodistico, [documental.Slug] = documental };

        var result = _resolver.ResolveChain(documental, bySlug);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Slug.Should().Be("periodistico");
        result.Value[1].Slug.Should().Be("documental");
    }

    [Fact]
    public void ResolveChain_SinPadre_DeberiaRetornarSoloElPerfil()
    {
        var humor = Profile.Create("humor", "Humor", null, 1, "h.md", "hash", DateTimeOffset.UtcNow).Value;
        var bySlug = new Dictionary<string, Profile> { [humor.Slug] = humor };

        var result = _resolver.ResolveChain(humor, bySlug);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }

    [Fact]
    public void ResolveChain_ConCicloEntreDosPerfiles_DeberiaFallar()
    {
        var a = Profile.Create("a", "A", "b", 1, "a.md", "hash", DateTimeOffset.UtcNow).Value;
        var b = Profile.Create("b", "B", "a", 1, "b.md", "hash", DateTimeOffset.UtcNow).Value;
        var bySlug = new Dictionary<string, Profile> { [a.Slug] = a, [b.Slug] = b };

        var result = _resolver.ResolveChain(a, bySlug);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Ciclo");
    }

    [Fact]
    public void ResolveChain_ConPadreInexistente_DeberiaFallar()
    {
        var huerfano = Profile.Create("huerfano", "Huérfano", "fantasma", 1, "h.md", "hash", DateTimeOffset.UtcNow).Value;
        var bySlug = new Dictionary<string, Profile> { [huerfano.Slug] = huerfano };

        var result = _resolver.ResolveChain(huerfano, bySlug);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no existe");
    }
}

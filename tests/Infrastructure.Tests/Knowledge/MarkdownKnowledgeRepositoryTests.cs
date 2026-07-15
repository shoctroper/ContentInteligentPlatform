using ContentIntelligencePlatform.Infrastructure.Knowledge;
using FluentAssertions;
using Xunit;

namespace ContentIntelligencePlatform.Infrastructure.Tests.Knowledge;

public class MarkdownKnowledgeRepositoryTests : IDisposable
{
    private readonly string _root;

    public MarkdownKnowledgeRepositoryTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "cip-knowledge-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(_root, "profiles"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private void WriteProfile(string fileName, string content) =>
        File.WriteAllText(Path.Combine(_root, "profiles", fileName), content);

    [Fact]
    public async Task GetAllProfilesAsync_ConPerfilesValidos_DeberiaParsearFrontMatterYCuerpo()
    {
        WriteProfile("periodistico.md", """
            ---
            slug: periodistico
            name: Periodístico
            version: 1
            ---
            Tono serio y objetivo.
            """);

        var repo = new MarkdownKnowledgeRepository(_root);
        var profiles = await repo.GetAllProfilesAsync(CancellationToken.None);

        profiles.Should().ContainSingle();
        profiles[0].Slug.Should().Be("periodistico");
        profiles[0].Name.Should().Be("Periodístico");
        profiles[0].ParentSlug.Should().BeNull();
        profiles[0].Version.Should().Be(1);
        profiles[0].ContentMarkdown.Should().Contain("Tono serio y objetivo.");
    }

    [Fact]
    public async Task GetAllProfilesAsync_ConHerencia_DeberiaExponerParentSlug()
    {
        WriteProfile("documental.md", """
            ---
            slug: documental
            name: Documental
            inherits: periodistico
            version: 1
            ---
            Narrativa pausada.
            """);

        var repo = new MarkdownKnowledgeRepository(_root);
        var profiles = await repo.GetAllProfilesAsync(CancellationToken.None);

        profiles[0].ParentSlug.Should().Be("periodistico");
    }

    [Fact]
    public async Task GetAllProfilesAsync_SinCarpetaProfiles_DeberiaRetornarListaVacia()
    {
        var emptyRoot = Path.Combine(Path.GetTempPath(), "cip-empty-" + Guid.NewGuid());
        Directory.CreateDirectory(emptyRoot);
        var repo = new MarkdownKnowledgeRepository(emptyRoot);

        var profiles = await repo.GetAllProfilesAsync(CancellationToken.None);

        profiles.Should().BeEmpty();
        Directory.Delete(emptyRoot);
    }

    [Fact]
    public async Task FindProfileAsync_ConSlugExistente_DeberiaRetornarlo()
    {
        WriteProfile("humor.md", """
            ---
            slug: humor
            name: Humor
            version: 1
            ---
            Tono ligero.
            """);

        var repo = new MarkdownKnowledgeRepository(_root);
        var found = await repo.FindProfileAsync("humor", CancellationToken.None);

        found.Should().NotBeNull();
        found!.Name.Should().Be("Humor");
    }

    [Fact]
    public async Task FindProfileAsync_ConSlugInexistente_DeberiaRetornarNull()
    {
        var repo = new MarkdownKnowledgeRepository(_root);
        var found = await repo.FindProfileAsync("no-existe", CancellationToken.None);

        found.Should().BeNull();
    }

    [Fact]
    public async Task ReadBaseKnowledgeAsync_ConArchivoExistente_DeberiaRetornarContenido()
    {
        await File.WriteAllTextAsync(Path.Combine(_root, "identity.md"), "Eres el motor editorial.");
        var repo = new MarkdownKnowledgeRepository(_root);

        var content = await repo.ReadBaseKnowledgeAsync("identity.md", CancellationToken.None);

        content.Should().Be("Eres el motor editorial.");
    }

    [Fact]
    public async Task ReadBaseKnowledgeAsync_ConArchivoInexistente_DeberiaRetornarVacio()
    {
        var repo = new MarkdownKnowledgeRepository(_root);

        var content = await repo.ReadBaseKnowledgeAsync("no-existe.md", CancellationToken.None);

        content.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllProfilesAsync_SinFrontMatter_DeberiaLanzarExcepcion()
    {
        WriteProfile("malformado.md", "Solo texto, sin front-matter.");
        var repo = new MarkdownKnowledgeRepository(_root);

        var act = async () => await repo.GetAllProfilesAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

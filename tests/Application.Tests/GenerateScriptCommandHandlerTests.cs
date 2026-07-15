using ContentIntelligencePlatform.Application.Abstractions;
using ContentIntelligencePlatform.Application.Generations.Commands;
using ContentIntelligencePlatform.Application.Tests.TestSupport;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ContentIntelligencePlatform.Application.Tests;

public class GenerateScriptCommandHandlerTests
{
    private const string ExtractionJson = """
        {"title":"Título","summary":"Resumen","facts":["Hecho 1","Hecho 2"],"confidence":0.9,"missingInformation":null}
        """;

    private const string ScriptJson = """
        {"title":"Título","hook":"Hook","introduction":"Intro","body":"Cuerpo","ending":"Cierre","cta":"Sigue","hashtags":["#a"],"keywords":["k"],"category":"news","estimatedDurationSeconds":60,"confidence":0.85,"missingInformation":null,"sources":["fuente"]}
        """;

    private static IKnowledgeRepository BuildKnowledgeRepository()
    {
        var repo = Substitute.For<IKnowledgeRepository>();
        var profileDoc = new KnowledgeProfileDocument("periodistico", "Periodístico", null, 1, "profiles/periodistico.md", "Tono serio.", "hash");

        repo.GetAllProfilesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeProfileDocument> { profileDoc });
        repo.ReadBaseKnowledgeAsync("identity.md", Arg.Any<CancellationToken>())
            .Returns("Eres el motor editorial.");

        return repo;
    }

    private static IAiProvider BuildAiProvider()
    {
        var provider = Substitute.For<IAiProvider>();
        provider.Name.Returns("Claude");
        provider.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new AiCompletionResult(ExtractionJson, 100, 50, 0.001m, "claude-test"),
                new AiCompletionResult(ScriptJson, 200, 100, 0.002m, "claude-test"));

        return provider;
    }

    [Fact]
    public async Task Handle_ConPerfilExistente_DeberiaGenerarScriptYPersistirlo()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var handler = new GenerateScriptCommandHandler(
            BuildAiProvider(), BuildKnowledgeRepository(), db, NullLogger<GenerateScriptCommandHandler>.Instance);

        var command = new GenerateScriptCommand("Texto de la fuente de prueba.", "periodistico", "TikTok");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProfileSlug.Should().Be("periodistico");
        result.Value.TokensInput.Should().Be(300);
        result.Value.TokensOutput.Should().Be(150);
        db.Generations.Should().HaveCount(1);
        db.NewsItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ConPerfilInexistente_DeberiaFallar()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var handler = new GenerateScriptCommandHandler(
            BuildAiProvider(), BuildKnowledgeRepository(), db, NullLogger<GenerateScriptCommandHandler>.Instance);

        var command = new GenerateScriptCommand("Texto", "no-existe", "TikTok");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no existe");
    }
}

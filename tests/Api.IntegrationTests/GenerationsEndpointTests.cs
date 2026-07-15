using System.Net;
using System.Net.Http.Json;
using ContentIntelligencePlatform.Application.Abstractions;
using ContentIntelligencePlatform.Application.Common;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ContentIntelligencePlatform.Api.IntegrationTests;

public class GenerationsEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string ExtractionJson = """
        {"title":"Título","summary":"Resumen","facts":["Hecho 1"],"confidence":0.9,"missingInformation":null}
        """;

    private const string ScriptJson = """
        {"title":"Título","hook":"Hook","introduction":"Intro","body":"Cuerpo","ending":"Cierre","cta":"Sigue","hashtags":["#a"],"keywords":["k"],"category":"news","estimatedDurationSeconds":60,"confidence":0.85,"missingInformation":null,"sources":["fuente"]}
        """;

    private readonly CustomWebApplicationFactory _factory;

    public GenerationsEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;

        var profileDoc = new KnowledgeProfileDocument("periodistico", "Periodístico", null, 1, "profiles/periodistico.md", "Tono serio.", "hash");
        _factory.FakeKnowledgeRepository.GetAllProfilesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeProfileDocument> { profileDoc });
        _factory.FakeKnowledgeRepository.ReadBaseKnowledgeAsync("identity.md", Arg.Any<CancellationToken>())
            .Returns("Eres el motor editorial.");

        _factory.FakeAiProvider.Name.Returns("Claude");
        _factory.FakeAiProvider.CompleteAsync(Arg.Any<AiCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new AiCompletionResult(ExtractionJson, 100, 50, 0.001m, "claude-test"),
                new AiCompletionResult(ScriptJson, 200, 100, 0.002m, "claude-test"));
    }

    [Fact]
    public async Task PostGeneration_ConPerfilExistente_DeberiaRetornar201YGuion()
    {
        var client = _factory.CreateClient();
        var request = new { sourceText = "Texto de prueba", profileSlug = "periodistico", outputFormat = "TikTok" };

        var response = await client.PostAsJsonAsync("/api/generations", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<GenerationDetailDto>();
        dto.Should().NotBeNull();
        dto!.ProfileSlug.Should().Be("periodistico");
    }

    [Fact]
    public async Task PostGeneration_ConCampoFaltante_DeberiaRetornar400()
    {
        var client = _factory.CreateClient();
        var request = new { sourceText = "", profileSlug = "periodistico", outputFormat = "TikTok" };

        var response = await client.PostAsJsonAsync("/api/generations", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostGeneration_ConPerfilInexistente_DeberiaRetornar422()
    {
        var client = _factory.CreateClient();
        var request = new { sourceText = "Texto válido", profileSlug = "perfil-inexistente", outputFormat = "TikTok" };

        var response = await client.PostAsJsonAsync("/api/generations", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}

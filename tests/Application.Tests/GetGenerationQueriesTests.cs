using ContentIntelligencePlatform.Application.Generations.Queries;
using ContentIntelligencePlatform.Application.Tests.TestSupport;
using ContentIntelligencePlatform.Domain.Generations;
using ContentIntelligencePlatform.Domain.NewsItems;
using ContentIntelligencePlatform.Domain.Profiles;
using ContentIntelligencePlatform.Domain.Sources;
using FluentAssertions;
using Xunit;

namespace ContentIntelligencePlatform.Application.Tests;

public class GetGenerationQueriesTests
{
    private static (Profile profile, NewsItem newsItem, Generation generation) SeedGeneration(
        ContentIntelligencePlatform.Infrastructure.Persistence.AppDbContext db)
    {
        var source = Source.Create(SourceType.FreeText, "texto", DateTimeOffset.UtcNow).Value;
        var newsItem = NewsItem.Create(source.Id, "T", "S", "[]", 0.9m, null, DateTimeOffset.UtcNow).Value;
        var profile = Profile.Create("periodistico", "Periodístico", null, 1, "p.md", "h", DateTimeOffset.UtcNow).Value;
        var generation = Generation.Create(newsItem.Id, profile.Id, "Claude", "model", "prompt", "md", "{}", 10, 20, 0.01m, 100, DateTimeOffset.UtcNow).Value;

        db.Sources.Add(source);
        db.NewsItems.Add(newsItem);
        db.Profiles.Add(profile);
        db.Generations.Add(generation);
        db.SaveChanges();

        return (profile, newsItem, generation);
    }

    [Fact]
    public async Task GetGenerationHistoryQuery_SinFiltro_DeberiaRetornarTodas()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var (_, _, generation) = SeedGeneration(db);

        var handler = new GetGenerationHistoryQueryHandler(db);
        var result = await handler.Handle(new GetGenerationHistoryQuery(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(g => g.Id == generation.Id);
    }

    [Fact]
    public async Task GetGenerationHistoryQuery_ConFiltroDePerfilQueNoCoincide_DeberiaRetornarVacio()
    {
        await using var db = InMemoryDbContextFactory.Create();
        SeedGeneration(db);

        var handler = new GetGenerationHistoryQueryHandler(db);
        var result = await handler.Handle(new GetGenerationHistoryQuery("otro-perfil"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGenerationByIdQuery_ConIdExistente_DeberiaRetornarDetalle()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var (_, _, generation) = SeedGeneration(db);

        var handler = new GetGenerationByIdQueryHandler(db);
        var result = await handler.Handle(new GetGenerationByIdQuery(generation.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProfileSlug.Should().Be("periodistico");
    }

    [Fact]
    public async Task GetGenerationByIdQuery_ConIdInexistente_DeberiaFallar()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var handler = new GetGenerationByIdQueryHandler(db);
        var result = await handler.Handle(new GetGenerationByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}

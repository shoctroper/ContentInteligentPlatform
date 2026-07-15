using ContentIntelligencePlatform.Application.Generations.Commands;
using ContentIntelligencePlatform.Application.Tests.TestSupport;
using ContentIntelligencePlatform.Domain.Generations;
using ContentIntelligencePlatform.Domain.NewsItems;
using ContentIntelligencePlatform.Domain.Profiles;
using ContentIntelligencePlatform.Domain.Sources;
using FluentAssertions;
using Xunit;

namespace ContentIntelligencePlatform.Application.Tests;

public class RateGenerationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ConGeneracionExistente_DeberiaCalificarla()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var source = Source.Create(SourceType.FreeText, "texto", DateTimeOffset.UtcNow).Value;
        var newsItem = NewsItem.Create(source.Id, "T", "S", "[]", 0.9m, null, DateTimeOffset.UtcNow).Value;
        var profile = Profile.Create("periodistico", "Periodístico", null, 1, "p.md", "h", DateTimeOffset.UtcNow).Value;
        var generation = Generation.Create(newsItem.Id, profile.Id, "Claude", "model", "prompt", "md", "{}", 10, 20, 0.01m, 100, DateTimeOffset.UtcNow).Value;

        db.Sources.Add(source);
        db.NewsItems.Add(newsItem);
        db.Profiles.Add(profile);
        db.Generations.Add(generation);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new RateGenerationCommandHandler(db);
        var result = await handler.Handle(new RateGenerationCommand(generation.Id, 5, "Excelente"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.Generations.FindAsync(generation.Id))!.Rating!.Value.Value.Should().Be(5);
    }

    [Fact]
    public async Task Handle_ConGeneracionInexistente_DeberiaFallar()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var handler = new RateGenerationCommandHandler(db);

        var result = await handler.Handle(new RateGenerationCommand(Guid.NewGuid(), 5, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}

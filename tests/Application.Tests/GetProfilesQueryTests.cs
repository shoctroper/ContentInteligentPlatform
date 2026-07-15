using ContentIntelligencePlatform.Application.Profiles.Queries;
using ContentIntelligencePlatform.Application.Tests.TestSupport;
using ContentIntelligencePlatform.Domain.Profiles;
using FluentAssertions;
using Xunit;

namespace ContentIntelligencePlatform.Application.Tests;

public class GetProfilesQueryTests
{
    [Fact]
    public async Task Handle_ConPerfilesExistentes_DeberiaRetornarlos()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var profile = Profile.Create("periodistico", "Periodístico", null, 1, "p.md", "h", DateTimeOffset.UtcNow).Value;
        db.Profiles.Add(profile);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProfilesQueryHandler(db);
        var result = await handler.Handle(new GetProfilesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(p => p.Slug == "periodistico");
    }

    [Fact]
    public async Task Handle_SinPerfiles_DeberiaRetornarListaVacia()
    {
        await using var db = InMemoryDbContextFactory.Create();

        var handler = new GetProfilesQueryHandler(db);
        var result = await handler.Handle(new GetProfilesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}

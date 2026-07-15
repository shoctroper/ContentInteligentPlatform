using ContentIntelligencePlatform.Domain.Generations;
using ContentIntelligencePlatform.Domain.NewsItems;
using ContentIntelligencePlatform.Domain.Profiles;
using ContentIntelligencePlatform.Domain.Sources;
using Microsoft.EntityFrameworkCore;

namespace ContentIntelligencePlatform.Application.Abstractions;

/// <summary>
/// Puerto de persistencia. La implementación real (EF Core + SQLite) vive en Infrastructure (ADR-002).
/// </summary>
public interface IAppDbContext
{
    DbSet<Profile> Profiles { get; }
    DbSet<Source> Sources { get; }
    DbSet<NewsItem> NewsItems { get; }
    DbSet<Generation> Generations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

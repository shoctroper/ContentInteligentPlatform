using ContentIntelligencePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentIntelligencePlatform.Application.Tests.TestSupport;

public static class InMemoryDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}

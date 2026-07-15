using ContentIntelligencePlatform.Application.Abstractions;
using ContentIntelligencePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace ContentIntelligencePlatform.Api.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public IAiProvider FakeAiProvider { get; } = Substitute.For<IAiProvider>();
    public IKnowledgeRepository FakeKnowledgeRepository { get; } = Substitute.For<IKnowledgeRepository>();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase($"api-tests-{Guid.NewGuid()}"));

            services.RemoveAll<IAiProvider>();
            services.AddSingleton(FakeAiProvider);

            services.RemoveAll<IKnowledgeRepository>();
            services.AddSingleton(FakeKnowledgeRepository);
        });
    }
}

using ContentIntelligencePlatform.Application.Abstractions;
using ContentIntelligencePlatform.Domain.Generations;
using ContentIntelligencePlatform.Domain.NewsItems;
using ContentIntelligencePlatform.Domain.Profiles;
using ContentIntelligencePlatform.Domain.Sources;
using Microsoft.EntityFrameworkCore;

namespace ContentIntelligencePlatform.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<NewsItem> NewsItems => Set<NewsItem>();
    public DbSet<Generation> Generations => Set<Generation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Profile>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Slug).IsRequired().HasMaxLength(100);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.HasIndex(p => new { p.Slug, p.Version }).IsUnique();
        });

        modelBuilder.Entity<Source>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.RawContent).IsRequired();
        });

        modelBuilder.Entity<NewsItem>(b =>
        {
            b.HasKey(n => n.Id);
            b.Property(n => n.Title).IsRequired().HasMaxLength(500);
            b.Property(n => n.Confidence)
                .HasConversion(c => c.Value, v => Confidence.Create(v).Value);
        });

        modelBuilder.Entity<Generation>(b =>
        {
            b.HasKey(g => g.Id);
            b.Property(g => g.ProviderName).IsRequired().HasMaxLength(100);
            b.Property(g => g.Rating)
                .HasConversion(
                    r => r.HasValue ? r.Value.Value : (int?)null,
                    v => v.HasValue ? Rating.Create(v.Value).Value : (Rating?)null);
        });
    }
}

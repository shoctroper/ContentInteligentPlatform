using ContentIntelligencePlatform.Application.Abstractions;
using ContentIntelligencePlatform.Application.Common;
using ContentIntelligencePlatform.Domain.Common;
using MediatR;

namespace ContentIntelligencePlatform.Application.Generations.Queries;

public record GetGenerationHistoryQuery(string? ProfileSlug) : IRequest<Result<IReadOnlyList<GenerationSummaryDto>>>;

public class GetGenerationHistoryQueryHandler : IRequestHandler<GetGenerationHistoryQuery, Result<IReadOnlyList<GenerationSummaryDto>>>
{
    private readonly IAppDbContext _db;

    public GetGenerationHistoryQueryHandler(IAppDbContext db) => _db = db;

    public Task<Result<IReadOnlyList<GenerationSummaryDto>>> Handle(GetGenerationHistoryQuery request, CancellationToken cancellationToken)
    {
        var profilesById = _db.Profiles.ToDictionary(p => p.Id, p => p.Slug);

        var query = _db.Generations.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(request.ProfileSlug))
        {
            var slug = request.ProfileSlug.Trim().ToLowerInvariant();
            query = query.Where(g => profilesById.TryGetValue(g.ProfileId, out var s) && s == slug);
        }

        var result = query
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new GenerationSummaryDto(g.Id, profilesById.GetValueOrDefault(g.ProfileId, "?"), g.CreatedAt, g.Rating?.Value))
            .ToList();

        return Task.FromResult(Result.Success<IReadOnlyList<GenerationSummaryDto>>(result));
    }
}

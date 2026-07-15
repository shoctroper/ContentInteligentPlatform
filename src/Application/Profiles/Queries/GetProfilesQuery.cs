using ContentIntelligencePlatform.Application.Abstractions;
using ContentIntelligencePlatform.Application.Common;
using ContentIntelligencePlatform.Domain.Common;
using MediatR;

namespace ContentIntelligencePlatform.Application.Profiles.Queries;

public record GetProfilesQuery : IRequest<Result<IReadOnlyList<ProfileDto>>>;

public class GetProfilesQueryHandler : IRequestHandler<GetProfilesQuery, Result<IReadOnlyList<ProfileDto>>>
{
    private readonly IAppDbContext _db;

    public GetProfilesQueryHandler(IAppDbContext db) => _db = db;

    public Task<Result<IReadOnlyList<ProfileDto>>> Handle(GetProfilesQuery request, CancellationToken cancellationToken)
    {
        var result = _db.Profiles
            .Select(p => new ProfileDto(p.Id, p.Slug, p.Name, p.ParentSlug, p.Version))
            .ToList();

        return Task.FromResult(Result.Success<IReadOnlyList<ProfileDto>>(result));
    }
}

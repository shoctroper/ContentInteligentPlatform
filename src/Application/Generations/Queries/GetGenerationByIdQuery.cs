using ContentIntelligencePlatform.Application.Abstractions;
using ContentIntelligencePlatform.Application.Common;
using ContentIntelligencePlatform.Domain.Common;
using MediatR;

namespace ContentIntelligencePlatform.Application.Generations.Queries;

public record GetGenerationByIdQuery(Guid Id) : IRequest<Result<GenerationDetailDto>>;

public class GetGenerationByIdQueryHandler : IRequestHandler<GetGenerationByIdQuery, Result<GenerationDetailDto>>
{
    private readonly IAppDbContext _db;

    public GetGenerationByIdQueryHandler(IAppDbContext db) => _db = db;

    public Task<Result<GenerationDetailDto>> Handle(GetGenerationByIdQuery request, CancellationToken cancellationToken)
    {
        var generation = _db.Generations.FirstOrDefault(g => g.Id == request.Id);
        if (generation is null)
            return Task.FromResult(Result.Failure<GenerationDetailDto>("La generación no existe."));

        var profileSlug = _db.Profiles.FirstOrDefault(p => p.Id == generation.ProfileId)?.Slug ?? "?";

        var dto = new GenerationDetailDto(
            generation.Id, profileSlug, generation.ProviderName, generation.ResultMarkdown, generation.ResultJson,
            0, null, generation.TokensInput, generation.TokensOutput, generation.CostUsd,
            generation.Rating?.Value, generation.CreatedAt);

        return Task.FromResult(Result.Success(dto));
    }
}

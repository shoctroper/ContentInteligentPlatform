using ContentIntelligencePlatform.Application.Abstractions;
using ContentIntelligencePlatform.Domain.Common;
using FluentValidation;
using MediatR;

namespace ContentIntelligencePlatform.Application.Generations.Commands;

public record RateGenerationCommand(Guid GenerationId, int Rating, string? Comments) : IRequest<Result>;

public class RateGenerationCommandValidator : AbstractValidator<RateGenerationCommand>
{
    public RateGenerationCommandValidator()
    {
        RuleFor(x => x.GenerationId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
    }
}

public class RateGenerationCommandHandler : IRequestHandler<RateGenerationCommand, Result>
{
    private readonly IAppDbContext _db;

    public RateGenerationCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(RateGenerationCommand request, CancellationToken cancellationToken)
    {
        var generation = _db.Generations.FirstOrDefault(g => g.Id == request.GenerationId);
        if (generation is null)
            return Result.Failure("La generación no existe.");

        var rateResult = generation.Rate(request.Rating, request.Comments);
        if (rateResult.IsFailure)
            return rateResult;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

using ContentIntelligencePlatform.Application.Abstractions;
using ContentIntelligencePlatform.Domain.Common;
using ContentIntelligencePlatform.Domain.Profiles;
using FluentValidation;
using MediatR;

namespace ContentIntelligencePlatform.Application.Profiles.Commands;

public record CreateProfileCommand(string Slug, string Name, string? ParentSlug, string FilePath, int Version) : IRequest<Result<Guid>>;

public class CreateProfileCommandValidator : AbstractValidator<CreateProfileCommand>
{
    public CreateProfileCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.FilePath).NotEmpty();
        RuleFor(x => x.Version).GreaterThanOrEqualTo(1);
    }
}

public class CreateProfileCommandHandler : IRequestHandler<CreateProfileCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;

    public CreateProfileCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateProfileCommand request, CancellationToken cancellationToken)
    {
        if (_db.Profiles.Any(p => p.Slug == request.Slug && p.Version == request.Version))
            return Result.Failure<Guid>($"Ya existe el perfil '{request.Slug}' versión {request.Version}.");

        var profileResult = Profile.Create(request.Slug, request.Name, request.ParentSlug, request.Version, request.FilePath, contentHash: string.Empty, DateTimeOffset.UtcNow);
        if (profileResult.IsFailure)
            return Result.Failure<Guid>(profileResult.Error!);

        _db.Profiles.Add(profileResult.Value);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(profileResult.Value.Id);
    }
}

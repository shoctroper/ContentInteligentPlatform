using System.Text.Json;
using ContentIntelligencePlatform.Application.Abstractions;
using ContentIntelligencePlatform.Application.Common;
using ContentIntelligencePlatform.Domain.Common;
using ContentIntelligencePlatform.Domain.Generations;
using ContentIntelligencePlatform.Domain.NewsItems;
using ContentIntelligencePlatform.Domain.Profiles;
using ContentIntelligencePlatform.Domain.Sources;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ContentIntelligencePlatform.Application.Generations.Commands;

public record GenerateScriptCommand(string SourceText, string ProfileSlug, string OutputFormat) : IRequest<Result<GenerationDetailDto>>;

public class GenerateScriptCommandValidator : AbstractValidator<GenerateScriptCommand>
{
    public GenerateScriptCommandValidator()
    {
        RuleFor(x => x.SourceText).NotEmpty().WithMessage("El texto de la fuente es obligatorio.");
        RuleFor(x => x.ProfileSlug).NotEmpty().WithMessage("El perfil es obligatorio.");
        RuleFor(x => x.OutputFormat).NotEmpty().WithMessage("El formato de salida es obligatorio.");
    }
}

public class GenerateScriptCommandHandler : IRequestHandler<GenerateScriptCommand, Result<GenerationDetailDto>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAiProvider _aiProvider;
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly IAppDbContext _db;
    private readonly ILogger<GenerateScriptCommandHandler> _logger;

    public GenerateScriptCommandHandler(
        IAiProvider aiProvider, IKnowledgeRepository knowledgeRepository, IAppDbContext db,
        ILogger<GenerateScriptCommandHandler> logger)
    {
        _aiProvider = aiProvider;
        _knowledgeRepository = knowledgeRepository;
        _db = db;
        _logger = logger;
    }

    public async Task<Result<GenerationDetailDto>> Handle(GenerateScriptCommand request, CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope("GenerateScript ProfileSlug={ProfileSlug}", request.ProfileSlug);

        // 1. Resolver perfil + cadena de herencia (secciones 10-11 del PRD)
        var allProfileDocs = await _knowledgeRepository.GetAllProfilesAsync(cancellationToken);
        var leafDoc = allProfileDocs.FirstOrDefault(p => p.Slug == request.ProfileSlug.Trim().ToLowerInvariant());
        if (leafDoc is null)
            return Result.Failure<GenerationDetailDto>($"El perfil '{request.ProfileSlug}' no existe en knowledge/profiles.");

        var chainResult = ResolveProfileChain(leafDoc, allProfileDocs);
        if (chainResult.IsFailure)
            return Result.Failure<GenerationDetailDto>(chainResult.Error!);

        var baseKnowledge = await ReadCombinedBaseKnowledgeAsync(cancellationToken);

        // 2. Crear Source
        var sourceResult = Source.Create(SourceType.FreeText, request.SourceText, DateTimeOffset.UtcNow);
        if (sourceResult.IsFailure)
            return Result.Failure<GenerationDetailDto>(sourceResult.Error!);
        var source = sourceResult.Value;
        _db.Sources.Add(source);

        // 3. Etapa 1-3 del pipeline: comprensión + extracción + validación (autoevaluada por el modelo)
        var extractionSystemPrompt = PromptBuilder.BuildSystemPrompt(baseKnowledge, chainResult.Value);
        var extractionUserPrompt = PromptBuilder.BuildExtractionUserPrompt(request.SourceText);
        var extractionCompletion = await _aiProvider.CompleteAsync(
            new AiCompletionRequest(extractionSystemPrompt, extractionUserPrompt), cancellationToken);

        var extraction = JsonSerializer.Deserialize<ExtractionResultDto>(
                LlmJsonResponse.StripMarkdownFence(extractionCompletion.Text), JsonOptions)
            ?? throw new InvalidOperationException("El proveedor de IA devolvió una extracción inválida.");

        var newsItemResult = NewsItem.Create(source.Id, extraction.Title, extraction.Summary,
            JsonSerializer.Serialize(extraction.Facts), extraction.Confidence, extraction.MissingInformation, DateTimeOffset.UtcNow);
        if (newsItemResult.IsFailure)
            return Result.Failure<GenerationDetailDto>(newsItemResult.Error!);
        var newsItem = newsItemResult.Value;
        _db.NewsItems.Add(newsItem);

        // 4-7. Planeación narrativa + redacción + autoevaluación
        var scriptSystemPrompt = extractionSystemPrompt;
        var scriptUserPrompt = PromptBuilder.BuildScriptUserPrompt(request.OutputFormat, extraction);
        var scriptCompletion = await _aiProvider.CompleteAsync(
            new AiCompletionRequest(scriptSystemPrompt, scriptUserPrompt), cancellationToken);

        var script = JsonSerializer.Deserialize<ScriptResultDto>(
                LlmJsonResponse.StripMarkdownFence(scriptCompletion.Text), JsonOptions)
            ?? throw new InvalidOperationException("El proveedor de IA devolvió un guion inválido.");

        // 7b. Salvaguarda: la etapa de redacción puede "olvidar" un vacío de información
        // detectado en la extracción aunque el prompt se lo pida explícitamente (visto en pruebas
        // reales con Gemini). No confiamos únicamente en que el modelo lo reporte dos veces:
        // combinamos ambas etapas para que el usuario final nunca pierda una señal de incertidumbre.
        var finalConfidence = Math.Min(extraction.Confidence, script.Confidence);
        var finalMissingInformation = CombineMissingInformation(extraction.MissingInformation, script.MissingInformation);

        // 8. Salida: persistir Generation
        var profileEntity = await FindOrCreateProfileEntityAsync(leafDoc, cancellationToken);

        var totalTokensInput = extractionCompletion.TokensInput + scriptCompletion.TokensInput;
        var totalTokensOutput = extractionCompletion.TokensOutput + scriptCompletion.TokensOutput;
        var totalCost = extractionCompletion.CostUsd + scriptCompletion.CostUsd;

        var resultMarkdown = ToMarkdown(script);
        var resultJson = JsonSerializer.Serialize(script, JsonOptions);

        var generationResult = Generation.Create(
            newsItem.Id, profileEntity.Id, _aiProvider.Name, extractionCompletion.Model,
            scriptUserPrompt, resultMarkdown, resultJson, totalTokensInput, totalTokensOutput, totalCost,
            durationMs: 0, DateTimeOffset.UtcNow);
        if (generationResult.IsFailure)
            return Result.Failure<GenerationDetailDto>(generationResult.Error!);

        var generation = generationResult.Value;
        _db.Generations.Add(generation);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generación {GenerationId} creada con confianza {Confidence}", generation.Id, finalConfidence);

        return Result.Success(new GenerationDetailDto(
            generation.Id, leafDoc.Slug, generation.ProviderName, resultMarkdown, resultJson,
            finalConfidence, finalMissingInformation, totalTokensInput, totalTokensOutput, totalCost,
            null, generation.CreatedAt));
    }

    private static string? CombineMissingInformation(string? fromExtraction, string? fromScript)
    {
        if (string.IsNullOrWhiteSpace(fromExtraction))
            return string.IsNullOrWhiteSpace(fromScript) ? null : fromScript;
        if (string.IsNullOrWhiteSpace(fromScript))
            return fromExtraction;
        if (string.Equals(fromExtraction.Trim(), fromScript.Trim(), StringComparison.OrdinalIgnoreCase))
            return fromScript;

        return $"{fromScript} | (detectado también en extracción: {fromExtraction})";
    }

    // Orden deliberado: por qué (manifesto) -> quién (identity) -> límites (rules) -> cómo pensar (thinking)
    // -> cómo narrar (storytelling) -> cómo suena (voice). Ver docs/architecture (pendiente ADR de este orden).
    private static readonly string[] BaseKnowledgeFiles =
    {
        "manifesto.md", "identity.md", "rules.md", "thinking.md", "storytelling.md", "voice.md"
    };

    private async Task<string> ReadCombinedBaseKnowledgeAsync(CancellationToken cancellationToken)
    {
        var parts = new List<string>();
        foreach (var file in BaseKnowledgeFiles)
        {
            var content = await _knowledgeRepository.ReadBaseKnowledgeAsync(file, cancellationToken);
            if (!string.IsNullOrWhiteSpace(content))
                parts.Add(content);
        }

        return string.Join("\n\n---\n\n", parts);
    }

    private async Task<Profile> FindOrCreateProfileEntityAsync(KnowledgeProfileDocument doc, CancellationToken cancellationToken)
    {
        var existing = _db.Profiles.FirstOrDefault(p => p.Slug == doc.Slug && p.Version == doc.Version);
        if (existing is not null)
            return existing;

        var created = Profile.Create(doc.Slug, doc.Name, doc.ParentSlug, doc.Version, doc.FilePath, doc.ContentHash, DateTimeOffset.UtcNow);
        _db.Profiles.Add(created.Value);
        await Task.CompletedTask;
        return created.Value;
    }

    private static Result<IReadOnlyList<KnowledgeProfileDocument>> ResolveProfileChain(
        KnowledgeProfileDocument leaf, IReadOnlyList<KnowledgeProfileDocument> all)
    {
        var bySlug = all.ToDictionary(p => p.Slug);
        var chain = new List<KnowledgeProfileDocument> { leaf };
        var visited = new HashSet<string> { leaf.Slug };
        var current = leaf;

        while (current.ParentSlug is not null)
        {
            if (!visited.Add(current.ParentSlug))
                return Result.Failure<IReadOnlyList<KnowledgeProfileDocument>>($"Ciclo de herencia detectado en '{current.ParentSlug}'.");
            if (!bySlug.TryGetValue(current.ParentSlug, out var parent))
                return Result.Failure<IReadOnlyList<KnowledgeProfileDocument>>($"El perfil padre '{current.ParentSlug}' no existe.");

            chain.Add(parent);
            current = parent;
        }

        chain.Reverse();
        return Result.Success<IReadOnlyList<KnowledgeProfileDocument>>(chain);
    }

    private static string ToMarkdown(ScriptResultDto script) =>
        $"""
         # {script.Title}

         **Hook:** {script.Hook}

         ## Introducción
         {script.Introduction}

         ## Cuerpo
         {script.Body}

         ## Cierre
         {script.Ending}

         **CTA:** {script.Cta}

         **Hashtags:** {string.Join(' ', script.Hashtags)}
         """;
}

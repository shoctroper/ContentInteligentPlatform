using ContentIntelligencePlatform.Application.Abstractions;
using ContentIntelligencePlatform.Application.Generations.Commands;
using ContentIntelligencePlatform.Application.Generations.Queries;
using ContentIntelligencePlatform.Application.Profiles.Commands;
using ContentIntelligencePlatform.Application.Profiles.Queries;
using ContentIntelligencePlatform.Infrastructure.AiProviders;
using ContentIntelligencePlatform.Infrastructure.Knowledge;
using ContentIntelligencePlatform.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Configuración ---
var knowledgeRoot = builder.Configuration["Knowledge:RootPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "..", "..", "knowledge");
var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=cip.db";

// --- Persistencia ---
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// --- Knowledge Engine ---
builder.Services.AddSingleton<IKnowledgeRepository>(_ => new MarkdownKnowledgeRepository(knowledgeRoot));

// --- AI Provider (ADR-004): hoy Claude, mañana intercambiable por configuración ---
builder.Services.AddSingleton(new ClaudeAiProviderOptions
{
    ApiKey = builder.Configuration["AiProvider:Claude:ApiKey"] ?? string.Empty,
    Model = builder.Configuration["AiProvider:Claude:Model"] ?? "claude-sonnet-4-5-20250929"
});
builder.Services.AddHttpClient<IAiProvider, ClaudeAiProvider>();

// --- CQRS / MediatR / FluentValidation ---
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(GenerateScriptCommand).Assembly);
    cfg.AddOpenBehavior(typeof(ContentIntelligencePlatform.Application.Common.ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(GenerateScriptCommand).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    var isValidation = feature?.Error is FluentValidation.ValidationException;
    var problem = new ProblemDetails
    {
        Status = isValidation ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError,
        Title = isValidation ? "Error de validación." : "Ocurrió un error inesperado.",
        Detail = feature?.Error.Message
    };
    context.Response.StatusCode = problem.Status.Value;
    await context.Response.WriteAsJsonAsync(problem);
}));

// --- Endpoints: Profiles ---
app.MapGet("/api/profiles", async (ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new GetProfilesQuery(), ct);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error);
});

app.MapPost("/api/profiles", async (CreateProfileCommand command, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(command, ct);
    return result.IsSuccess
        ? Results.Created($"/api/profiles/{result.Value}", new { id = result.Value })
        : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
});

// --- Endpoints: Generations ---
app.MapGet("/api/generations", async (string? profileSlug, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new GetGenerationHistoryQuery(profileSlug), ct);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error);
});

app.MapPost("/api/generations", async (GenerateScriptCommand command, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(command, ct);
    return result.IsSuccess
        ? Results.Created($"/api/generations/{result.Value.Id}", result.Value)
        : Results.Problem(result.Error, statusCode: StatusCodes.Status422UnprocessableEntity);
});

app.MapGet("/api/generations/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new GetGenerationByIdQuery(id), ct);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
});

app.MapPatch("/api/generations/{id:guid}/rating", async (Guid id, RateGenerationRequest body, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new RateGenerationCommand(id, body.Rating, body.Comments), ct);
    return result.IsSuccess ? Results.NoContent() : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
});

app.Run();

public record RateGenerationRequest(int Rating, string? Comments);

// Necesario para que WebApplicationFactory<Program> funcione en los tests de integración.
public partial class Program { }

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
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

// --- AI Provider (ADR-004 + ADR-007): intercambiable por configuración AiProvider:Active ---
builder.Services.AddSingleton(new ClaudeAiProviderOptions
{
    ApiKey = builder.Configuration["AiProvider:Claude:ApiKey"] ?? string.Empty,
    Model = builder.Configuration["AiProvider:Claude:Model"] ?? "claude-sonnet-4-5-20250929"
});
builder.Services.AddSingleton(new DeepSeekAiProviderOptions
{
    ApiKey = builder.Configuration["AiProvider:DeepSeek:ApiKey"] ?? string.Empty,
    Model = builder.Configuration["AiProvider:DeepSeek:Model"] ?? "deepseek-v4-flash"
});
builder.Services.AddHttpClient("Claude");
builder.Services.AddHttpClient("DeepSeek");

var activeAiProvider = builder.Configuration["AiProvider:Active"] ?? "Claude";
builder.Services.AddScoped<IAiProvider>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return activeAiProvider switch
    {
        "DeepSeek" => new DeepSeekAiProvider(httpClientFactory.CreateClient("DeepSeek"), sp.GetRequiredService<DeepSeekAiProviderOptions>()),
        "Claude" => new ClaudeAiProvider(httpClientFactory.CreateClient("Claude"), sp.GetRequiredService<ClaudeAiProviderOptions>()),
        _ => throw new InvalidOperationException(
            $"Proveedor de IA desconocido en AiProvider:Active: '{activeAiProvider}'. Valores soportados: Claude, DeepSeek.")
    };
});

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

// --- CORS: habilitado para el frontend Angular (ADR-006). Ajustar orígenes antes de producción real. ---
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
});

// --- Observabilidad (OpenTelemetry, sección "Observabilidad" de la skill devops) ---
var otlpEndpoint = builder.Configuration["Otel:Endpoint"];
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("content-intelligence-api"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            t.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
        else
            t.AddConsoleExporter();
    })
    .WithMetrics(m =>
    {
        m.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation();
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            m.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    });

builder.Logging.AddOpenTelemetry(o =>
{
    o.IncludeScopes = true;
    o.IncludeFormattedMessage = true;
});

// --- Health checks: /health (liveness) y /ready (readiness, incluye DB) ---
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: new[] { "ready" });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");

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

app.Use(async (context, next) =>
{
    const string headerName = "X-Correlation-Id";
    var correlationId = context.Request.Headers.TryGetValue(headerName, out var existing)
        ? existing.ToString()
        : Guid.NewGuid().ToString();
    context.Response.Headers[headerName] = correlationId;

    using (context.RequestServices.GetRequiredService<ILoggerFactory>()
               .CreateLogger("CorrelationId").BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
    {
        await next();
    }
});

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();

public record RateGenerationRequest(int Rating, string? Comments);

// Necesario para que WebApplicationFactory<Program> funcione en los tests de integración.
public partial class Program { }

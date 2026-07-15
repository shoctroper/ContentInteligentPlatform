using System.Net;
using ContentIntelligencePlatform.Application.Abstractions;
using ContentIntelligencePlatform.Infrastructure.AiProviders;
using ContentIntelligencePlatform.Infrastructure.Tests.TestSupport;
using FluentAssertions;
using Xunit;

namespace ContentIntelligencePlatform.Infrastructure.Tests.AiProviders;

public class OpenRouterAiProviderTests
{
    private const string SuccessResponse = """
        {
          "id": "gen-123",
          "model": "openai/gpt-4o-mini",
          "choices": [{"message": {"role": "assistant", "content": "{\"title\":\"T\"}"}}],
          "usage": {"prompt_tokens": 500, "completion_tokens": 120, "total_tokens": 620, "cost": 0.00034}
        }
        """;

    private const string SuccessResponseSinCosto = """
        {
          "model": "openai/gpt-4o-mini",
          "choices": [{"message": {"content": "{\"title\":\"T\"}"}}],
          "usage": {"prompt_tokens": 500, "completion_tokens": 120}
        }
        """;

    [Fact]
    public async Task CompleteAsync_SinApiKeyConfigurada_DeberiaLanzarExcepcionExplicita()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessResponse);
        var httpClient = new HttpClient(handler);
        var provider = new OpenRouterAiProvider(httpClient, new OpenRouterAiProviderOptions { ApiKey = "" });

        var act = async () => await provider.CompleteAsync(new AiCompletionRequest("system", "user"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*API key*");
    }

    [Fact]
    public async Task CompleteAsync_ConRespuestaExitosa_UsaElCostoNativoDeOpenRouter()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessResponse);
        var httpClient = new HttpClient(handler);
        var provider = new OpenRouterAiProvider(httpClient, new OpenRouterAiProviderOptions { ApiKey = "fake-key" });

        var result = await provider.CompleteAsync(new AiCompletionRequest("system", "user"), CancellationToken.None);

        result.Text.Should().Be("{\"title\":\"T\"}");
        result.TokensInput.Should().Be(500);
        result.TokensOutput.Should().Be(120);
        result.CostUsd.Should().Be(0.00034m);
        result.Model.Should().Be("openai/gpt-4o-mini");
    }

    [Fact]
    public async Task CompleteAsync_SinCampoCost_UsaCeroComoFallback()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessResponseSinCosto);
        var httpClient = new HttpClient(handler);
        var provider = new OpenRouterAiProvider(httpClient, new OpenRouterAiProviderOptions { ApiKey = "fake-key" });

        var result = await provider.CompleteAsync(new AiCompletionRequest("system", "user"), CancellationToken.None);

        result.CostUsd.Should().Be(0m);
    }

    [Fact]
    public async Task CompleteAsync_EnviaHeadersOpcionalesDeIdentificacion()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessResponse);
        var httpClient = new HttpClient(handler);
        var provider = new OpenRouterAiProvider(httpClient, new OpenRouterAiProviderOptions
        {
            ApiKey = "fake-key",
            SiteUrl = "https://example.com",
            SiteName = "Mi App"
        });

        await provider.CompleteAsync(new AiCompletionRequest("system", "user"), CancellationToken.None);

        handler.LastRequest!.Headers.GetValues("HTTP-Referer").Should().Contain("https://example.com");
        handler.LastRequest!.Headers.GetValues("X-Title").Should().Contain("Mi App");
    }

    [Fact]
    public async Task CompleteAsync_ConRespuestaDeError_DeberiaLanzarExcepcionConDetalle()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.PaymentRequired, "{\"error\":{\"message\":\"Insufficient credits\"}}");
        var httpClient = new HttpClient(handler);
        var provider = new OpenRouterAiProvider(httpClient, new OpenRouterAiProviderOptions { ApiKey = "fake-key" });

        var act = async () => await provider.CompleteAsync(new AiCompletionRequest("system", "user"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*402*");
    }
}

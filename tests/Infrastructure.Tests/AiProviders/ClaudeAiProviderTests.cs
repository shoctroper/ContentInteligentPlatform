using System.Net;
using ContentIntelligencePlatform.Application.Abstractions;
using ContentIntelligencePlatform.Infrastructure.AiProviders;
using ContentIntelligencePlatform.Infrastructure.Tests.TestSupport;
using FluentAssertions;
using Xunit;

namespace ContentIntelligencePlatform.Infrastructure.Tests.AiProviders;

public class ClaudeAiProviderTests
{
    private const string SuccessResponse = """
        {
          "content": [{"type": "text", "text": "{\"title\":\"T\"}"}],
          "usage": {"input_tokens": 120, "output_tokens": 40}
        }
        """;

    [Fact]
    public async Task CompleteAsync_SinApiKeyConfigurada_DeberiaLanzarExcepcionExplicita()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessResponse);
        var httpClient = new HttpClient(handler);
        var provider = new ClaudeAiProvider(httpClient, new ClaudeAiProviderOptions { ApiKey = "" });

        var act = async () => await provider.CompleteAsync(new AiCompletionRequest("system", "user"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*API key*");
    }

    [Fact]
    public async Task CompleteAsync_ConRespuestaExitosa_DeberiaParsearTextoYTokensYCalcularCosto()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessResponse);
        var httpClient = new HttpClient(handler);
        var options = new ClaudeAiProviderOptions
        {
            ApiKey = "fake-key",
            Model = "claude-test",
            InputCostPerMillionTokens = 3.0m,
            OutputCostPerMillionTokens = 15.0m
        };
        var provider = new ClaudeAiProvider(httpClient, options);

        var result = await provider.CompleteAsync(new AiCompletionRequest("system", "user"), CancellationToken.None);

        result.Text.Should().Be("{\"title\":\"T\"}");
        result.TokensInput.Should().Be(120);
        result.TokensOutput.Should().Be(40);
        result.Model.Should().Be("claude-test");
        // (120/1e6 * 3.0) + (40/1e6 * 15.0) = 0.00036 + 0.0006 = 0.00096
        result.CostUsd.Should().Be(0.00096m);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Headers.GetValues("x-api-key").Should().Contain("fake-key");
    }

    [Fact]
    public async Task CompleteAsync_ConRespuestaDeError_DeberiaLanzarExcepcionConDetalle()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.TooManyRequests, "{\"error\":\"rate limited\"}");
        var httpClient = new HttpClient(handler);
        var provider = new ClaudeAiProvider(httpClient, new ClaudeAiProviderOptions { ApiKey = "fake-key" });

        var act = async () => await provider.CompleteAsync(new AiCompletionRequest("system", "user"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*429*");
    }
}

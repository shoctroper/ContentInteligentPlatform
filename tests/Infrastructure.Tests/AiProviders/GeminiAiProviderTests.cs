using System.Net;
using ContentIntelligencePlatform.Application.Abstractions;
using ContentIntelligencePlatform.Infrastructure.AiProviders;
using ContentIntelligencePlatform.Infrastructure.Tests.TestSupport;
using FluentAssertions;
using Xunit;

namespace ContentIntelligencePlatform.Infrastructure.Tests.AiProviders;

public class GeminiAiProviderTests
{
    private const string SuccessResponse = """
        {
          "model": "gemini-3.5-flash",
          "steps": [
            {
              "type": "model_output",
              "content": [{"type": "text", "text": "{\"title\":\"T\"}"}]
            }
          ],
          "usage": {
            "total_input_tokens": 100,
            "total_output_tokens": 40,
            "total_thought_tokens": 10,
            "total_tokens": 150
          }
        }
        """;

    private const string SuccessResponseMultiplesBloquesDeTexto = """
        {
          "model": "gemini-3.5-flash",
          "steps": [
            {
              "type": "model_output",
              "content": [
                {"type": "text", "text": "{\"title\":"},
                {"type": "text", "text": "\"T\"}"}
              ]
            }
          ],
          "usage": {"total_input_tokens": 50, "total_output_tokens": 20, "total_thought_tokens": 0}
        }
        """;

    [Fact]
    public async Task CompleteAsync_SinApiKeyConfigurada_DeberiaLanzarExcepcionExplicita()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessResponse);
        var httpClient = new HttpClient(handler);
        var provider = new GeminiAiProvider(httpClient, new GeminiAiProviderOptions { ApiKey = "" });

        var act = async () => await provider.CompleteAsync(new AiCompletionRequest("system", "user"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*API key*");
    }

    [Fact]
    public async Task CompleteAsync_ConRespuestaExitosa_ExtraeTextoYSumaThoughtTokensAlOutput()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessResponse);
        var httpClient = new HttpClient(handler);
        var options = new GeminiAiProviderOptions
        {
            ApiKey = "fake-key",
            InputCostPerMillionTokens = 1.50m,
            OutputCostPerMillionTokens = 9.00m
        };
        var provider = new GeminiAiProvider(httpClient, options);

        var result = await provider.CompleteAsync(new AiCompletionRequest("system", "user"), CancellationToken.None);

        result.Text.Should().Be("{\"title\":\"T\"}");
        result.TokensInput.Should().Be(100);
        result.TokensOutput.Should().Be(50); // 40 output + 10 thought
        // 100/1e6*1.50 + 50/1e6*9.00 = 0.00015 + 0.00045 = 0.0006
        result.CostUsd.Should().Be(0.0006m);
        result.Model.Should().Be("gemini-3.5-flash");
    }

    [Fact]
    public async Task CompleteAsync_ConMultiplesBloquesDeTexto_LosConcatena()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessResponseMultiplesBloquesDeTexto);
        var httpClient = new HttpClient(handler);
        var provider = new GeminiAiProvider(httpClient, new GeminiAiProviderOptions { ApiKey = "fake-key" });

        var result = await provider.CompleteAsync(new AiCompletionRequest("system", "user"), CancellationToken.None);

        result.Text.Should().Be("{\"title\":\"T\"}");
    }

    [Fact]
    public async Task CompleteAsync_EnviaHeaderXGoogApiKey()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessResponse);
        var httpClient = new HttpClient(handler);
        var provider = new GeminiAiProvider(httpClient, new GeminiAiProviderOptions { ApiKey = "fake-key" });

        await provider.CompleteAsync(new AiCompletionRequest("system", "user"), CancellationToken.None);

        handler.LastRequest!.Headers.GetValues("x-goog-api-key").Should().Contain("fake-key");
    }

    [Fact]
    public async Task CompleteAsync_ConRespuestaDeError_DeberiaLanzarExcepcionConDetalle()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Forbidden, "{\"error\":{\"message\":\"API key invalid\"}}");
        var httpClient = new HttpClient(handler);
        var provider = new GeminiAiProvider(httpClient, new GeminiAiProviderOptions { ApiKey = "fake-key" });

        var act = async () => await provider.CompleteAsync(new AiCompletionRequest("system", "user"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*403*");
    }
}

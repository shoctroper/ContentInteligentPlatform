using System.Net;
using ContentIntelligencePlatform.Application.Abstractions;
using ContentIntelligencePlatform.Infrastructure.AiProviders;
using ContentIntelligencePlatform.Infrastructure.Tests.TestSupport;
using FluentAssertions;
using Xunit;

namespace ContentIntelligencePlatform.Infrastructure.Tests.AiProviders;

public class DeepSeekAiProviderTests
{
    private const string SuccessResponseSinDetalleDeCache = """
        {
          "choices": [{"message": {"content": "{\"title\":\"T\"}"}}],
          "usage": {"prompt_tokens": 1000, "completion_tokens": 200}
        }
        """;

    private const string SuccessResponseConCacheHit = """
        {
          "choices": [{"message": {"content": "{\"title\":\"T\"}"}}],
          "usage": {"prompt_tokens": 1000, "completion_tokens": 200, "prompt_cache_hit_tokens": 800, "prompt_cache_miss_tokens": 200}
        }
        """;

    [Fact]
    public async Task CompleteAsync_SinApiKeyConfigurada_DeberiaLanzarExcepcionExplicita()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessResponseSinDetalleDeCache);
        var httpClient = new HttpClient(handler);
        var provider = new DeepSeekAiProvider(httpClient, new DeepSeekAiProviderOptions { ApiKey = "" });

        var act = async () => await provider.CompleteAsync(new AiCompletionRequest("system", "user"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*API key*");
    }

    [Fact]
    public async Task CompleteAsync_SinDetalleDeCache_AsumeTodoCacheMiss()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessResponseSinDetalleDeCache);
        var httpClient = new HttpClient(handler);
        var options = new DeepSeekAiProviderOptions
        {
            ApiKey = "fake-key",
            Model = "deepseek-v4-flash",
            CacheHitInputCostPerMillionTokens = 0.0028m,
            CacheMissInputCostPerMillionTokens = 0.14m,
            OutputCostPerMillionTokens = 0.28m
        };
        var provider = new DeepSeekAiProvider(httpClient, options);

        var result = await provider.CompleteAsync(new AiCompletionRequest("system", "user"), CancellationToken.None);

        result.Text.Should().Be("{\"title\":\"T\"}");
        result.TokensInput.Should().Be(1000);
        result.TokensOutput.Should().Be(200);
        // 1000/1e6 * 0.14 + 200/1e6 * 0.28 = 0.00014 + 0.000056 = 0.000196
        result.CostUsd.Should().Be(0.000196m);
    }

    [Fact]
    public async Task CompleteAsync_ConDetalleDeCache_CalculaCostoMixto()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessResponseConCacheHit);
        var httpClient = new HttpClient(handler);
        var options = new DeepSeekAiProviderOptions
        {
            ApiKey = "fake-key",
            CacheHitInputCostPerMillionTokens = 0.0028m,
            CacheMissInputCostPerMillionTokens = 0.14m,
            OutputCostPerMillionTokens = 0.28m
        };
        var provider = new DeepSeekAiProvider(httpClient, options);

        var result = await provider.CompleteAsync(new AiCompletionRequest("system", "user"), CancellationToken.None);

        // 800/1e6*0.0028 + 200/1e6*0.14 + 200/1e6*0.28 = 0.00000224 + 0.000028 + 0.000056 = 0.00008624
        result.CostUsd.Should().Be(0.00008624m);
    }

    [Fact]
    public async Task CompleteAsync_EnviaThinkingDisabledPorDefecto()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SuccessResponseSinDetalleDeCache);
        var httpClient = new HttpClient(handler);
        var provider = new DeepSeekAiProvider(httpClient, new DeepSeekAiProviderOptions { ApiKey = "fake-key" });

        await provider.CompleteAsync(new AiCompletionRequest("system", "user"), CancellationToken.None);

        handler.LastRequestBody.Should().Contain("\"type\":\"disabled\"");
    }

    [Fact]
    public async Task CompleteAsync_ConRespuestaDeError_DeberiaLanzarExcepcionConDetalle()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized, "{\"error\":\"invalid api key\"}");
        var httpClient = new HttpClient(handler);
        var provider = new DeepSeekAiProvider(httpClient, new DeepSeekAiProviderOptions { ApiKey = "fake-key" });

        var act = async () => await provider.CompleteAsync(new AiCompletionRequest("system", "user"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*401*");
    }
}

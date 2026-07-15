using ContentIntelligencePlatform.Application.Common;
using FluentAssertions;
using Xunit;

namespace ContentIntelligencePlatform.Application.Tests;

public class LlmJsonResponseTests
{
    [Fact]
    public void StripMarkdownFence_ConJsonPlano_NoLoModifica()
    {
        var result = LlmJsonResponse.StripMarkdownFence("{\"a\":1}");
        result.Should().Be("{\"a\":1}");
    }

    [Fact]
    public void StripMarkdownFence_ConFenceConLenguaje_LoRemueve()
    {
        var input = "```json\n{\"a\":1}\n```";
        var result = LlmJsonResponse.StripMarkdownFence(input);
        result.Should().Be("{\"a\":1}");
    }

    [Fact]
    public void StripMarkdownFence_ConFenceSinLenguaje_LoRemueve()
    {
        var input = "```\n{\"a\":1}\n```";
        var result = LlmJsonResponse.StripMarkdownFence(input);
        result.Should().Be("{\"a\":1}");
    }

    [Fact]
    public void StripMarkdownFence_ConEspaciosAlrededor_LosLimpia()
    {
        var input = "  \n```json\n{\"a\":1}\n```\n  ";
        var result = LlmJsonResponse.StripMarkdownFence(input);
        result.Should().Be("{\"a\":1}");
    }
}

using System.Net;
using System.Net.Http.Json;
using ContentIntelligencePlatform.Application.Common;
using FluentAssertions;
using Xunit;

namespace ContentIntelligencePlatform.Api.IntegrationTests;

public class ProfilesEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProfilesEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetProfiles_SinPerfiles_DeberiaRetornarListaVacia()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/profiles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profiles = await response.Content.ReadFromJsonAsync<List<ProfileDto>>();
        profiles.Should().NotBeNull();
    }

    [Fact]
    public async Task PostProfile_ConDatosValidos_DeberiaCrearlo()
    {
        var client = _factory.CreateClient();
        var request = new { slug = "test-profile", name = "Test Profile", parentSlug = (string?)null, filePath = "profiles/test.md", version = 1 };

        var response = await client.PostAsJsonAsync("/api/profiles", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostProfile_ConSlugVacio_DeberiaRetornar400()
    {
        var client = _factory.CreateClient();
        var request = new { slug = "", name = "Test Profile", parentSlug = (string?)null, filePath = "profiles/test.md", version = 1 };

        var response = await client.PostAsJsonAsync("/api/profiles", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

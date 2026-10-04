using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SeriesApp.Api.Tests;

/// <summary>
/// Boots the real API host in-memory and asserts the host wiring works end to end.
/// With no feature modules registered yet, the health endpoint is the whole surface.
/// </summary>
public sealed class HealthEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}

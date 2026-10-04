using System.Net;

namespace SeriesApp.Api.Tests;

/// <summary>
/// Boots the real API host in-memory and asserts the host wiring works end to end, including
/// that module registration and its startup option validation do not break the host.
/// </summary>
public sealed class HealthEndpointTests(SeriesAppApiFactory factory) : IClassFixture<SeriesAppApiFactory>
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

using System.Net;
using System.Text.Json;
using SeriesApp.Modules.Catalog;
using SeriesApp.Modules.Catalog.Search;

namespace SeriesApp.Api.Tests;

/// <summary>
/// Drives <c>GET /api/catalog/search</c> through the real host with a fake catalog provider,
/// covering request binding, validation, the serialized contract, and failure translation.
/// </summary>
public sealed class CatalogSearchEndpointTests : IClassFixture<SeriesAppApiFactory>
{
    private readonly SeriesAppApiFactory _factory;

    public CatalogSearchEndpointTests(SeriesAppApiFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _factory = factory;
        _factory.CatalogProvider.Reset();
    }

    [Fact]
    public async Task Search_returns_the_provider_results_in_our_contract()
    {
        _factory.CatalogProvider.Page = new CatalogSearchPage(
            1,
            3,
            42,
            [
                new CatalogSearchItem(
                    "1396",
                    CatalogMediaType.Tv,
                    "Breaking Bad",
                    "Breaking Bad",
                    "A chemistry teacher turned methamphetamine producer.",
                    "/series-poster.jpg",
                    "/series-backdrop.jpg",
                    new DateOnly(2008, 1, 20),
                    8.9,
                    14321),
            ]);

        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri("/api/catalog/search?query=breaking%20bad", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("page").GetInt32());
        Assert.Equal(3, root.GetProperty("totalPages").GetInt32());
        Assert.Equal(42, root.GetProperty("totalResults").GetInt32());

        var result = root.GetProperty("results")[0];

        Assert.Equal("1396", result.GetProperty("externalId").GetString());
        Assert.Equal("tv", result.GetProperty("mediaType").GetString());
        Assert.Equal("Breaking Bad", result.GetProperty("title").GetString());
        Assert.Equal("/series-poster.jpg", result.GetProperty("posterPath").GetString());
        Assert.Equal("2008-01-20", result.GetProperty("releaseDate").GetString());
        Assert.Equal(8.9, result.GetProperty("voteAverage").GetDouble());
        Assert.Equal(14321, result.GetProperty("voteCount").GetInt32());
    }

    [Fact]
    public async Task Search_passes_the_decoded_query_and_defaults_to_the_first_page()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri("/api/catalog/search?query=fire%20%26%20ice", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("fire & ice", _factory.CatalogProvider.LastQuery);
        Assert.Equal(1, _factory.CatalogProvider.LastPage);
    }

    [Fact]
    public async Task Search_forwards_the_requested_page()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri("/api/catalog/search?query=breaking%20bad&page=4", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(4, _factory.CatalogProvider.LastPage);
    }

    [Theory]
    [InlineData("/api/catalog/search")]
    [InlineData("/api/catalog/search?query=")]
    [InlineData("/api/catalog/search?query=%20%20%20")]
    public async Task Search_rejects_a_missing_or_blank_query(string requestUri)
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri(requestUri, UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.True(document.RootElement.GetProperty("errors").TryGetProperty("query", out _));
        Assert.Null(_factory.CatalogProvider.LastQuery);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(501)]
    public async Task Search_rejects_a_page_outside_the_supported_range(int page)
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri($"/api/catalog/search?query=breaking%20bad&page={page}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.True(document.RootElement.GetProperty("errors").TryGetProperty("page", out _));
        Assert.Null(_factory.CatalogProvider.LastPage);
    }

    [Fact]
    public async Task Search_reports_both_validation_failures_at_once()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri("/api/catalog/search?query=&page=0", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = document.RootElement.GetProperty("errors");

        Assert.True(errors.TryGetProperty("query", out _));
        Assert.True(errors.TryGetProperty("page", out _));
    }

    [Fact]
    public async Task Search_reports_a_provider_failure_as_a_bad_gateway_without_upstream_detail()
    {
        _factory.CatalogProvider.Failure = new CatalogProviderException(
            "The catalog provider could not be reached.",
            new HttpRequestException("api.themoviedb.org refused the secret-sauce-12345 credential"));

        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri("/api/catalog/search?query=breaking%20bad", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Catalog provider unavailable", body, StringComparison.Ordinal);
        Assert.DoesNotContain("themoviedb", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret-sauce-12345", body, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpRequestException", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Openapi_document_describes_the_catalog_search_endpoint()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var operation = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/catalog/search")
            .GetProperty("get");

        Assert.Equal("SearchCatalog", operation.GetProperty("operationId").GetString());

        var responses = operation.GetProperty("responses");

        Assert.True(responses.TryGetProperty("200", out _));
        Assert.True(responses.TryGetProperty("400", out _));
        Assert.True(responses.TryGetProperty("502", out _));

        var parameters = operation.GetProperty("parameters")
            .EnumerateArray()
            .Select(parameter => parameter.GetProperty("name").GetString())
            .ToList();

        Assert.Contains("query", parameters);
        Assert.Contains("page", parameters);
    }
}

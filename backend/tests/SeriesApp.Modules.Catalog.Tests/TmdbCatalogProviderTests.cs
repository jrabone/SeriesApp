using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging.Abstractions;
using SeriesApp.Modules.Catalog.Search;
using SeriesApp.Modules.Catalog.Tmdb;

namespace SeriesApp.Modules.Catalog.Tests;

/// <summary>
/// Covers the TMDB adapter against a stubbed transport. Nothing here touches the network, and
/// the only token in play is the obvious placeholder below.
/// </summary>
public sealed class TmdbCatalogProviderTests
{
    private const string PlaceholderToken = "not-a-real-token";

    /// <summary>
    /// A representative multi-search page: a series, a film, a person (dropped), a film whose
    /// optional fields are all blank, and a film with no title at all (dropped).
    /// </summary>
    private const string MultiSearchPayload = """
        {
          "page": 2,
          "results": [
            {
              "id": 1396,
              "media_type": "tv",
              "name": "Breaking Bad",
              "original_name": "Breaking Bad",
              "overview": "A chemistry teacher turned methamphetamine producer.",
              "poster_path": "/series-poster.jpg",
              "backdrop_path": "/series-backdrop.jpg",
              "first_air_date": "2008-01-20",
              "vote_average": 8.9,
              "vote_count": 14321
            },
            {
              "id": 155,
              "media_type": "movie",
              "title": "The Dark Knight",
              "original_title": "The Dark Knight",
              "overview": "Batman raises the stakes in his war on crime.",
              "poster_path": "/film-poster.jpg",
              "backdrop_path": null,
              "release_date": "2008-07-16",
              "vote_average": 8.5,
              "vote_count": 32107
            },
            {
              "id": 17419,
              "media_type": "person",
              "name": "Bryan Cranston",
              "known_for_department": "Acting"
            },
            {
              "id": 999,
              "media_type": "movie",
              "title": "Unscheduled Sequel",
              "original_title": "",
              "overview": "",
              "poster_path": null,
              "backdrop_path": null,
              "release_date": "",
              "vote_average": 0,
              "vote_count": 0
            },
            {
              "id": 1000,
              "media_type": "movie",
              "original_title": "Nameless"
            }
          ],
          "total_pages": 7,
          "total_results": 134
        }
        """;

    private const string EmptyPayload = """
        {"page":1,"results":[],"total_pages":0,"total_results":0}
        """;

    private static readonly Uri BaseAddress = new("https://tmdb.invalid/3/");

    [Fact]
    public async Task Search_maps_series_results()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, MultiSearchPayload);
        using var client = CreateClient(handler);
        var provider = new TmdbCatalogProvider(client, NullLogger<TmdbCatalogProvider>.Instance);

        var page = await provider.SearchAsync("breaking bad", 2, CancellationToken.None);

        var series = page.Results.Single(result => result.MediaType == CatalogMediaType.Tv);

        Assert.Equal("1396", series.ExternalId);
        Assert.Equal("Breaking Bad", series.Title);
        Assert.Equal("Breaking Bad", series.OriginalTitle);
        Assert.Equal("A chemistry teacher turned methamphetamine producer.", series.Overview);
        Assert.Equal("/series-poster.jpg", series.PosterPath);
        Assert.Equal("/series-backdrop.jpg", series.BackdropPath);
        Assert.Equal(new DateOnly(2008, 1, 20), series.ReleaseDate);
        Assert.Equal(8.9, series.VoteAverage);
        Assert.Equal(14321, series.VoteCount);
    }

    [Fact]
    public async Task Search_maps_film_results()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, MultiSearchPayload);
        using var client = CreateClient(handler);
        var provider = new TmdbCatalogProvider(client, NullLogger<TmdbCatalogProvider>.Instance);

        var page = await provider.SearchAsync("dark knight", 2, CancellationToken.None);

        var film = page.Results.Single(result => result.ExternalId == "155");

        Assert.Equal(CatalogMediaType.Movie, film.MediaType);
        Assert.Equal("The Dark Knight", film.Title);
        Assert.Equal("The Dark Knight", film.OriginalTitle);
        Assert.Equal("/film-poster.jpg", film.PosterPath);
        Assert.Null(film.BackdropPath);
        Assert.Equal(new DateOnly(2008, 7, 16), film.ReleaseDate);
        Assert.Equal(8.5, film.VoteAverage);
        Assert.Equal(32107, film.VoteCount);
    }

    [Fact]
    public async Task Search_drops_people_and_unnameable_results()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, MultiSearchPayload);
        using var client = CreateClient(handler);
        var provider = new TmdbCatalogProvider(client, NullLogger<TmdbCatalogProvider>.Instance);

        var page = await provider.SearchAsync("breaking bad", 2, CancellationToken.None);

        var ids = page.Results.Select(result => result.ExternalId).ToList();

        Assert.Equal(3, page.Results.Count);
        Assert.DoesNotContain("17419", ids);
        Assert.DoesNotContain("1000", ids);
        Assert.All(
            page.Results,
            result => Assert.True(result.MediaType is CatalogMediaType.Movie or CatalogMediaType.Tv));
    }

    [Fact]
    public async Task Search_normalizes_blank_dates_images_and_text_to_null()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, MultiSearchPayload);
        using var client = CreateClient(handler);
        var provider = new TmdbCatalogProvider(client, NullLogger<TmdbCatalogProvider>.Instance);

        var page = await provider.SearchAsync("sequel", 2, CancellationToken.None);

        var sparse = page.Results.Single(result => result.ExternalId == "999");

        Assert.Null(sparse.ReleaseDate);
        Assert.Null(sparse.OriginalTitle);
        Assert.Null(sparse.Overview);
        Assert.Null(sparse.PosterPath);
        Assert.Null(sparse.BackdropPath);
    }

    [Fact]
    public async Task Search_passes_the_provider_paging_through_unchanged()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, MultiSearchPayload);
        using var client = CreateClient(handler);
        var provider = new TmdbCatalogProvider(client, NullLogger<TmdbCatalogProvider>.Instance);

        var page = await provider.SearchAsync("breaking bad", 2, CancellationToken.None);

        Assert.Equal(2, page.Page);
        Assert.Equal(7, page.TotalPages);
        Assert.Equal(134, page.TotalResults);
    }

    [Fact]
    public async Task Search_requests_multi_search_with_an_escaped_query()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, EmptyPayload);
        using var client = CreateClient(handler);
        var provider = new TmdbCatalogProvider(client, NullLogger<TmdbCatalogProvider>.Instance);

        await provider.SearchAsync("fire & ice", 3, CancellationToken.None);

        Assert.Equal(1, handler.SendCount);
        Assert.Equal(
            "/3/search/multi?query=fire%20%26%20ice&page=3&include_adult=false",
            handler.LastRequestUri?.PathAndQuery);
    }

    [Fact]
    public async Task Search_authenticates_with_a_bearer_token()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, EmptyPayload);
        using var client = CreateClient(handler);
        var provider = new TmdbCatalogProvider(client, NullLogger<TmdbCatalogProvider>.Instance);

        await provider.SearchAsync("breaking bad", 1, CancellationToken.None);

        Assert.Equal("Bearer", handler.LastAuthorization?.Scheme);
        Assert.Equal(PlaceholderToken, handler.LastAuthorization?.Parameter);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Search_translates_an_unsuccessful_status_into_a_provider_failure(HttpStatusCode statusCode)
    {
        using var handler = new StubHttpMessageHandler(statusCode, """{"status_message":"nope"}""");
        using var client = CreateClient(handler);
        var provider = new TmdbCatalogProvider(client, NullLogger<TmdbCatalogProvider>.Instance);

        var failure = await Assert.ThrowsAsync<CatalogProviderException>(
            () => provider.SearchAsync("breaking bad", 1, CancellationToken.None));

        // The upstream body is not carried in anything a caller could ever see.
        Assert.DoesNotContain("nope", failure.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{ this is not json")]
    [InlineData("<html>maintenance</html>")]
    [InlineData("")]
    public async Task Search_translates_an_unreadable_payload_into_a_provider_failure(string body)
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = CreateClient(handler);
        var provider = new TmdbCatalogProvider(client, NullLogger<TmdbCatalogProvider>.Instance);

        await Assert.ThrowsAsync<CatalogProviderException>(
            () => provider.SearchAsync("breaking bad", 1, CancellationToken.None));
    }

    [Fact]
    public async Task Search_translates_a_transport_failure_into_a_provider_failure()
    {
        using var handler = new StubHttpMessageHandler(new HttpRequestException("no such host"));
        using var client = CreateClient(handler);
        var provider = new TmdbCatalogProvider(client, NullLogger<TmdbCatalogProvider>.Instance);

        var failure = await Assert.ThrowsAsync<CatalogProviderException>(
            () => provider.SearchAsync("breaking bad", 1, CancellationToken.None));

        Assert.IsType<HttpRequestException>(failure.InnerException);
    }

    [Fact]
    public async Task Search_translates_a_timeout_into_a_provider_failure()
    {
        using var handler = new StubHttpMessageHandler(
            new TaskCanceledException("timed out", new TimeoutException()));
        using var client = CreateClient(handler);
        var provider = new TmdbCatalogProvider(client, NullLogger<TmdbCatalogProvider>.Instance);

        await Assert.ThrowsAsync<CatalogProviderException>(
            () => provider.SearchAsync("breaking bad", 1, CancellationToken.None));
    }

    [Fact]
    public async Task Search_leaves_caller_cancellation_as_a_cancellation()
    {
        using var handler = new StubHttpMessageHandler(new TaskCanceledException("cancelled"));
        using var client = CreateClient(handler);
        var provider = new TmdbCatalogProvider(client, NullLogger<TmdbCatalogProvider>.Instance);
        using var cancellation = new CancellationTokenSource();

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.SearchAsync("breaking bad", 1, cancellation.Token));
    }

    [Fact]
    public async Task Search_never_leaks_the_token_in_a_failure()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.Unauthorized, "{}");
        using var client = CreateClient(handler);
        var provider = new TmdbCatalogProvider(client, NullLogger<TmdbCatalogProvider>.Instance);

        var failure = await Assert.ThrowsAsync<CatalogProviderException>(
            () => provider.SearchAsync("breaking bad", 1, CancellationToken.None));

        Assert.DoesNotContain(PlaceholderToken, failure.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer", failure.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_rejects_a_blank_query_before_calling_out(string query)
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, EmptyPayload);
        using var client = CreateClient(handler);
        var provider = new TmdbCatalogProvider(client, NullLogger<TmdbCatalogProvider>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(
            () => provider.SearchAsync(query, 1, CancellationToken.None));

        Assert.Equal(0, handler.SendCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Search_rejects_a_page_below_one_before_calling_out(int page)
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, EmptyPayload);
        using var client = CreateClient(handler);
        var provider = new TmdbCatalogProvider(client, NullLogger<TmdbCatalogProvider>.Instance);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => provider.SearchAsync("breaking bad", page, CancellationToken.None));

        Assert.Equal(0, handler.SendCount);
    }

    private static HttpClient CreateClient(HttpMessageHandler handler) =>
        new(handler, disposeHandler: false)
        {
            BaseAddress = BaseAddress,
            DefaultRequestHeaders =
            {
                Authorization = new AuthenticationHeaderValue("Bearer", PlaceholderToken),
            },
        };
}

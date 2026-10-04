using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using SeriesApp.Modules.Catalog.Search;

namespace SeriesApp.Modules.Catalog.Tmdb;

/// <summary>
/// An <see cref="ICatalogProvider"/> backed by TMDB's multi-search API.
/// </summary>
/// <remarks>
/// The bearer token is attached to the typed client once, in <see cref="CatalogModule"/>, and
/// is never read, logged, or echoed here. Every upstream failure is translated into a
/// <see cref="CatalogProviderException"/>, so no third-party exception type crosses out of
/// the provider.
/// </remarks>
public sealed class TmdbCatalogProvider(HttpClient httpClient, ILogger<TmdbCatalogProvider> logger)
    : ICatalogProvider
{
    private const string MovieMediaType = "movie";
    private const string TvMediaType = "tv";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <inheritdoc />
    public async Task<CatalogSearchPage> SearchAsync(string query, int page, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);

        var requestUri = new Uri(
            QueryHelpers.AddQueryString(
                "search/multi",
                new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["query"] = query,
                    ["page"] = page.ToString(CultureInfo.InvariantCulture),
                    ["include_adult"] = "false",
                }),
            UriKind.Relative);

        TmdbSearchResponse? payload;

        try
        {
            using var response = await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                // Status code only. TMDB's error body is not something we want in our logs,
                // and it is certainly not something we want in a response.
                logger.LogWarning(
                    "TMDB multi-search answered with status {StatusCode}.",
                    (int)response.StatusCode);

                throw new CatalogProviderException("The catalog provider returned an unsuccessful response.");
            }

            payload = await response.Content
                .ReadFromJsonAsync<TmdbSearchResponse>(SerializerOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new CatalogProviderException("The catalog provider could not be reached.", ex);
        }
        catch (JsonException ex)
        {
            throw new CatalogProviderException("The catalog provider returned an unreadable response.", ex);
        }
        catch (NotSupportedException ex)
        {
            // A success status carrying something that is not JSON — a proxy or error page.
            throw new CatalogProviderException("The catalog provider returned an unreadable response.", ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient surfaces its own timeout as a cancellation. A caller who walked away
            // is not a provider failure, so that case is deliberately left to propagate.
            throw new CatalogProviderException("The catalog provider did not respond in time.", ex);
        }

        if (payload is null)
        {
            throw new CatalogProviderException("The catalog provider returned an empty response.");
        }

        return new CatalogSearchPage(
            payload.Page,
            payload.TotalPages,
            payload.TotalResults,
            [.. payload.Results.Select(ToCatalogItem).OfType<CatalogSearchItem>()]);
    }

    /// <summary>
    /// Maps one TMDB entry onto our contract, or returns <see langword="null"/> for entries
    /// this iteration does not expose.
    /// </summary>
    private static CatalogSearchItem? ToCatalogItem(TmdbSearchResult result)
    {
        CatalogMediaType? mediaType = result.MediaType switch
        {
            MovieMediaType => CatalogMediaType.Movie,
            TvMediaType => CatalogMediaType.Tv,
            // People — and anything TMDB adds to multi-search later — are out of scope.
            _ => null,
        };

        if (mediaType is null)
        {
            return null;
        }

        var isMovie = mediaType.Value == CatalogMediaType.Movie;
        var title = isMovie ? result.Title : result.Name;

        if (string.IsNullOrWhiteSpace(title))
        {
            // A result we cannot name is of no use to a client.
            return null;
        }

        return new CatalogSearchItem(
            result.Id.ToString(CultureInfo.InvariantCulture),
            mediaType.Value,
            title,
            NullIfBlank(isMovie ? result.OriginalTitle : result.OriginalName),
            NullIfBlank(result.Overview),
            NullIfBlank(result.PosterPath),
            NullIfBlank(result.BackdropPath),
            ParseDate(isMovie ? result.ReleaseDate : result.FirstAirDate),
            result.VoteAverage,
            result.VoteCount);
    }

    /// <summary>TMDB uses empty strings where it means "nothing"; our contract uses null.</summary>
    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    /// <summary>Best-effort date parse: TMDB sends an empty string for an unknown date.</summary>
    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
}

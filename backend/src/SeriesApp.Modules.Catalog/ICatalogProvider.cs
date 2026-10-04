using SeriesApp.Modules.Catalog.Search;

namespace SeriesApp.Modules.Catalog;

/// <summary>
/// A source of catalog metadata about films and television series.
/// </summary>
/// <remarks>
/// The module's endpoints depend on this abstraction rather than on any particular upstream
/// service, so the provider can be replaced, stubbed in tests, or wrapped in a cache without
/// the endpoints or the response contracts changing.
/// </remarks>
public interface ICatalogProvider
{
    /// <summary>
    /// Searches the catalog for films and television series matching <paramref name="query"/>.
    /// </summary>
    /// <param name="query">Free-text search term. Must not be blank.</param>
    /// <param name="page">One-based page number.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The requested page of results, which may be empty.</returns>
    /// <exception cref="CatalogProviderException">
    /// The provider could not be reached, refused the request, or answered with something unusable.
    /// </exception>
    Task<CatalogSearchPage> SearchAsync(string query, int page, CancellationToken cancellationToken);
}

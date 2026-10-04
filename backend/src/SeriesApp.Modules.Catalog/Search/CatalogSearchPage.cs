namespace SeriesApp.Modules.Catalog.Search;

/// <summary>
/// One page of catalog search results.
/// </summary>
/// <param name="Page">The page number these results came from.</param>
/// <param name="TotalPages">Total pages available for the query, as reported by the provider.</param>
/// <param name="TotalResults">
/// Total matches for the query, as reported by the provider. The provider counts entries we
/// do not expose (people), so a page can hold fewer items than this total implies. It is
/// passed through unchanged so paging stays aligned with the provider's own pages.
/// </param>
/// <param name="Results">The films and series on this page.</param>
public sealed record CatalogSearchPage(
    int Page,
    int TotalPages,
    int TotalResults,
    IReadOnlyList<CatalogSearchItem> Results);

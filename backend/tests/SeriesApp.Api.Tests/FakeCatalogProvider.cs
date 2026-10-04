using SeriesApp.Modules.Catalog;
using SeriesApp.Modules.Catalog.Search;

namespace SeriesApp.Api.Tests;

/// <summary>
/// An <see cref="ICatalogProvider"/> that answers from memory, so endpoint tests exercise the
/// host wiring — binding, validation, serialization, status codes — without a network call.
/// </summary>
public sealed class FakeCatalogProvider : ICatalogProvider
{
    /// <summary>The page to answer with, when <see cref="Failure"/> is not set.</summary>
    public CatalogSearchPage Page { get; set; } = new(1, 0, 0, []);

    /// <summary>When set, the provider fails with this instead of answering.</summary>
    public Exception? Failure { get; set; }

    public string? LastQuery { get; private set; }

    public int? LastPage { get; private set; }

    public Task<CatalogSearchPage> SearchAsync(string query, int page, CancellationToken cancellationToken)
    {
        LastQuery = query;
        LastPage = page;

        return Failure is null
            ? Task.FromResult(Page)
            : Task.FromException<CatalogSearchPage>(Failure);
    }

    /// <summary>Returns the fake to its default answer, so one test cannot bleed into the next.</summary>
    public void Reset()
    {
        Page = new CatalogSearchPage(1, 0, 0, []);
        Failure = null;
        LastQuery = null;
        LastPage = null;
    }
}

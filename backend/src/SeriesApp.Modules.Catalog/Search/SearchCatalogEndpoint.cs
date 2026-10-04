using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace SeriesApp.Modules.Catalog.Search;

/// <summary>
/// <c>GET /api/catalog/search</c> — find films and television series by free text.
/// </summary>
internal static class SearchCatalogEndpoint
{
    /// <summary>
    /// Upper page bound. The provider caps paging of its own accord and answers with an error
    /// past the cap; rejecting it here keeps that a client mistake (400) instead of surfacing
    /// as an upstream failure (502).
    /// </summary>
    private const int MaxPage = 500;

    private const int DefaultPage = 1;

    public static IEndpointRouteBuilder MapSearchCatalog(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/search", SearchAsync)
            .WithName("SearchCatalog")
            .WithSummary("Searches the catalog for films and television series.")
            .WithDescription(
                "Free-text search across films and series; people are excluded. Paging mirrors " +
                "the upstream provider's pages, so a page may hold fewer items than the " +
                "reported totals imply.")
            // The typed result union documents 200 and 400 on its own, but a problem result
            // cannot infer our gateway status.
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return endpoints;
    }

    private static async Task<Results<Ok<CatalogSearchPage>, ValidationProblem, ProblemHttpResult>> SearchAsync(
        ICatalogProvider catalogProvider,
        ILogger<CatalogModule> logger,
        CancellationToken cancellationToken,
        string? query = null,
        int? page = null)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var requestedPage = page ?? DefaultPage;

        if (string.IsNullOrWhiteSpace(query))
        {
            errors[nameof(query)] = ["A search query is required and cannot be blank."];
        }

        if (requestedPage < 1)
        {
            errors[nameof(page)] = ["Page must be 1 or greater."];
        }
        else if (requestedPage > MaxPage)
        {
            errors[nameof(page)] = [$"Page must be {MaxPage} or lower."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        try
        {
            var results = await catalogProvider
                .SearchAsync(query!, requestedPage, cancellationToken)
                .ConfigureAwait(false);

            return TypedResults.Ok(results);
        }
        catch (CatalogProviderException ex)
        {
            // The cause is logged in full server-side; none of it goes back to the caller.
            logger.LogError(ex, "Catalog search failed: the catalog provider was unusable.");

            return TypedResults.Problem(
                title: "Catalog provider unavailable",
                detail: "The catalog could not be searched right now. Please try again later.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}

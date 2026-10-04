using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SeriesApp.Modules.Catalog.Search;
using SeriesApp.Modules.Catalog.Tmdb;
using SeriesApp.SharedKernel;

namespace SeriesApp.Modules.Catalog;

/// <summary>
/// Catalog: searching and browsing films and television series.
/// </summary>
/// <remarks>
/// The module owns its upstream integration behind <see cref="ICatalogProvider"/>. Nothing
/// TMDB-shaped is exposed outside it, and no other module is referenced.
/// </remarks>
public sealed class CatalogModule : IModule
{
    private const string RoutePrefix = "/api/catalog";

    /// <summary>Request headers that must never reach HttpClient's logs, at any level.</summary>
    private static readonly string[] RedactedHeaders = ["Authorization"];

    /// <inheritdoc />
    public string Name => "Catalog";

    /// <inheritdoc />
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Validated at startup, so a missing token is a boot failure with a clear message
        // rather than a run of confusing gateway errors later.
        services.AddOptions<TmdbOptions>()
            .Bind(configuration.GetSection(TmdbOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<ICatalogProvider, TmdbCatalogProvider>(ConfigureTmdbClient)
            .RedactLoggedHeaders(RedactedHeaders);

        return services;
    }

    /// <inheritdoc />
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGroup(RoutePrefix)
            .WithTags("Catalog")
            .MapSearchCatalog();

        return endpoints;
    }

    private static void ConfigureTmdbClient(IServiceProvider serviceProvider, HttpClient client)
    {
        var options = serviceProvider.GetRequiredService<IOptions<TmdbOptions>>().Value;

        client.BaseAddress = options.BaseAddress;

        // The one place the token is touched. It goes straight onto the client's default
        // headers and is never stored, logged, or passed on anywhere else.
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", options.ReadAccessToken);

        client.Timeout = TimeSpan.FromSeconds(10);
    }
}

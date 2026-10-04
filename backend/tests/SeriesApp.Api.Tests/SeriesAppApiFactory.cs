using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SeriesApp.Modules.Catalog;

namespace SeriesApp.Api.Tests;

/// <summary>
/// Boots the real API host in-memory with every outbound dependency replaced.
/// </summary>
/// <remarks>
/// Two things make the host reproducible on any machine. The catalog configuration is
/// overridden with placeholder values, because the Catalog module validates its options at
/// startup and would otherwise only boot where a real token happens to be installed; and
/// <see cref="ICatalogProvider"/> is swapped for a fake, so no test can reach the real
/// provider even if the configuration were wrong. The environment is pinned to Development
/// because the OpenAPI document is only served there.
/// </remarks>
public sealed class SeriesAppApiFactory : WebApplicationFactory<Program>
{
    /// <summary>The catalog stand-in each test arranges before calling the endpoint.</summary>
    public FakeCatalogProvider CatalogProvider { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Development);

        // Added last so it wins over user secrets: tests must never pick up a real token, and
        // the unroutable base address means a stray client could not call out if it tried.
        builder.ConfigureAppConfiguration(configuration =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Tmdb:ReadAccessToken"] = "placeholder-token-for-tests",
                ["Tmdb:BaseAddress"] = "https://tmdb.invalid/3/",
            }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICatalogProvider>();
            services.AddSingleton<ICatalogProvider>(CatalogProvider);
        });
    }
}

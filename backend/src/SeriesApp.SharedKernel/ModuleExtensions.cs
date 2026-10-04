using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SeriesApp.SharedKernel;

/// <summary>
/// Host-side helpers that fold a set of <see cref="IModule"/> instances into the
/// application's service container and routing table.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>Registers the services of every supplied module.</summary>
    public static IServiceCollection AddModules(
        this IServiceCollection services,
        IConfiguration configuration,
        IReadOnlyList<IModule> modules)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(modules);

        foreach (var module in modules)
        {
            module.RegisterServices(services, configuration);
        }

        return services;
    }

    /// <summary>Maps the endpoints of every supplied module.</summary>
    public static IEndpointRouteBuilder MapModules(
        this IEndpointRouteBuilder endpoints,
        IReadOnlyList<IModule> modules)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(modules);

        foreach (var module in modules)
        {
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }
}

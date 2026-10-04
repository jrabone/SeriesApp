using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SeriesApp.SharedKernel;

/// <summary>
/// A vertical feature module of the SeriesApp monolith.
/// </summary>
/// <remarks>
/// Each module owns its services, endpoints and persistence, and is deployed as part
/// of the single API host. Modules never reference each other directly; anything that
/// must cross a module boundary belongs in this shared kernel as a contract.
/// </remarks>
public interface IModule
{
    /// <summary>Stable identifier used in logs and diagnostics.</summary>
    string Name { get; }

    /// <summary>Registers the module's services into the host container.</summary>
    IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>Maps the module's HTTP endpoints onto the host's routing table.</summary>
    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}

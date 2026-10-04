using SeriesApp.Modules.Catalog;
using SeriesApp.SharedKernel;

namespace SeriesApp.Api;

/// <summary>
/// The single place where feature modules are plugged into the monolith.
/// </summary>
/// <remarks>
/// Registration is an explicit list rather than assembly scanning, so the set of
/// active modules is compile-time checked and visible at a glance. Add new modules
/// here as they are introduced.
/// </remarks>
internal static class ModuleRegistry
{
    public static IReadOnlyList<IModule> Modules { get; } = [new CatalogModule()];
}

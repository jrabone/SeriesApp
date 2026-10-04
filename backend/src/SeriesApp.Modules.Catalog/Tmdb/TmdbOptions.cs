using System.ComponentModel.DataAnnotations;

namespace SeriesApp.Modules.Catalog.Tmdb;

/// <summary>
/// Configuration for the TMDB-backed catalog provider.
/// </summary>
/// <remarks>
/// <see cref="ReadAccessToken"/> is a secret. It comes from .NET user secrets in development
/// and from the host's configuration provider elsewhere — never from a file in source control.
/// It is read exactly once, when the typed <see cref="HttpClient"/> is built, and is never
/// logged, serialized, or returned.
/// </remarks>
public sealed class TmdbOptions
{
    /// <summary>The configuration section these options bind to.</summary>
    public const string SectionName = "Tmdb";

    /// <summary>TMDB API read access token, sent as a bearer token.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Tmdb:ReadAccessToken is not configured.")]
    public string ReadAccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Root of the TMDB REST API. Configurable so tests and mirrors can point elsewhere; the
    /// trailing slash matters, because request paths are relative to it.
    /// </summary>
    [Required]
    public Uri BaseAddress { get; init; } = new("https://api.themoviedb.org/3/");
}

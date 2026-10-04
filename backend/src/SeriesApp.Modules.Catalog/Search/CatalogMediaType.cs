using System.Text.Json.Serialization;

namespace SeriesApp.Modules.Catalog.Search;

/// <summary>
/// The kind of catalog entry a search result refers to.
/// </summary>
/// <remarks>
/// Serialized as a string so the API contract reads the same way clients write it, and so
/// adding a member later cannot silently shift the meaning of an existing numeric value.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<CatalogMediaType>))]
public enum CatalogMediaType
{
    /// <summary>A film.</summary>
    [JsonStringEnumMemberName("movie")]
    Movie,

    /// <summary>A television series.</summary>
    [JsonStringEnumMemberName("tv")]
    Tv,
}

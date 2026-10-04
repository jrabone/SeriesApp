namespace SeriesApp.Modules.Catalog.Tmdb;

/// <summary>
/// TMDB's <c>/search/multi</c> response envelope.
/// </summary>
/// <remarks>
/// These types mirror TMDB's wire format and stay internal on purpose: nothing
/// provider-shaped is allowed to escape the module. Names are mapped from <c>snake_case</c>
/// by the serializer options in <see cref="TmdbCatalogProvider"/>, so no per-property
/// attributes are needed.
/// </remarks>
internal sealed record TmdbSearchResponse
{
    public int Page { get; init; }

    public IReadOnlyList<TmdbSearchResult> Results { get; init; } = [];

    public int TotalPages { get; init; }

    public int TotalResults { get; init; }
}

/// <summary>
/// One entry of a TMDB multi-search response. Multi-search returns films, series and people
/// in a single heterogeneous array, so every field that belongs to only one of them is optional.
/// </summary>
internal sealed record TmdbSearchResult
{
    public int Id { get; init; }

    /// <summary>Discriminator: <c>movie</c>, <c>tv</c> or <c>person</c>.</summary>
    public string? MediaType { get; init; }

    // Films carry title / original_title / release_date...
    public string? Title { get; init; }

    public string? OriginalTitle { get; init; }

    /// <summary>Kept as text: TMDB sends an empty string for an unknown date.</summary>
    public string? ReleaseDate { get; init; }

    // ...series carry name / original_name / first_air_date.
    public string? Name { get; init; }

    public string? OriginalName { get; init; }

    /// <summary>Kept as text: TMDB sends an empty string for an unknown date.</summary>
    public string? FirstAirDate { get; init; }

    public string? Overview { get; init; }

    public string? PosterPath { get; init; }

    public string? BackdropPath { get; init; }

    public double VoteAverage { get; init; }

    public int VoteCount { get; init; }
}

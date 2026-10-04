namespace SeriesApp.Modules.Catalog.Search;

/// <summary>
/// A single catalog search result, in SeriesApp's own shape.
/// </summary>
/// <param name="ExternalId">
/// Identifier assigned by the upstream catalog provider. A string rather than a number so
/// the contract survives a provider whose identifiers are not integers.
/// </param>
/// <param name="MediaType">Whether this result is a film or a television series.</param>
/// <param name="Title">Localized title. Films and series are normalized onto this one field.</param>
/// <param name="OriginalTitle">Title in the original language, when the provider reports one.</param>
/// <param name="Overview">Short synopsis, when the provider reports one.</param>
/// <param name="PosterPath">
/// Provider-relative poster path, exactly as reported. Composing absolute image URLs needs
/// the provider's image configuration document and arrives with the images iteration.
/// </param>
/// <param name="BackdropPath">Provider-relative backdrop path, exactly as reported.</param>
/// <param name="ReleaseDate">
/// Release date for films, first air date for series. Null when the provider does not know it.
/// </param>
/// <param name="VoteAverage">Average rating reported by the provider.</param>
/// <param name="VoteCount">Number of votes behind <paramref name="VoteAverage"/>.</param>
public sealed record CatalogSearchItem(
    string ExternalId,
    CatalogMediaType MediaType,
    string Title,
    string? OriginalTitle,
    string? Overview,
    string? PosterPath,
    string? BackdropPath,
    DateOnly? ReleaseDate,
    double VoteAverage,
    int VoteCount);

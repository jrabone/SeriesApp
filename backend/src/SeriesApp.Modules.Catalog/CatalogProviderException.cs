namespace SeriesApp.Modules.Catalog;

/// <summary>
/// Raised when an <see cref="ICatalogProvider"/> cannot satisfy a request.
/// </summary>
/// <remarks>
/// The message is always SeriesApp's own. Upstream detail — status codes, provider payloads,
/// transport errors — travels in <see cref="Exception.InnerException"/> for server-side logging
/// only, so nothing provider-specific can reach an API consumer.
/// </remarks>
public sealed class CatalogProviderException : Exception
{
    /// <summary>Creates the exception with a generic message.</summary>
    public CatalogProviderException()
        : base("The catalog provider could not satisfy the request.")
    {
    }

    /// <summary>Creates the exception with our own message.</summary>
    public CatalogProviderException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with our own message, keeping the upstream cause for logs.</summary>
    public CatalogProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

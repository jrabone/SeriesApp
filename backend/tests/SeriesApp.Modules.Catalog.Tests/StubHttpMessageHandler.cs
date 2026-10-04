using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace SeriesApp.Modules.Catalog.Tests;

/// <summary>
/// Stands in for the network: records what it was asked for, then replays a canned response
/// or throws a canned exception. Keeps provider tests entirely off TMDB.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpResponseMessage> _respond;

    /// <summary>Replays a response with the given status and body.</summary>
    public StubHttpMessageHandler(HttpStatusCode statusCode, string body, string mediaType = "application/json")
    {
        _respond = () => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, mediaType),
        };
    }

    /// <summary>Fails the send the way a transport or timeout failure would.</summary>
    public StubHttpMessageHandler(Exception failure)
    {
        _respond = () => throw failure;
    }

    /// <summary>
    /// Captured at send time rather than held as the request object, which HttpClient disposes
    /// once the call completes.
    /// </summary>
    public Uri? LastRequestUri { get; private set; }

    public AuthenticationHeaderValue? LastAuthorization { get; private set; }

    public int SendCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        SendCount++;
        LastRequestUri = request.RequestUri;
        LastAuthorization = request.Headers.Authorization;

        return Task.FromResult(_respond());
    }
}

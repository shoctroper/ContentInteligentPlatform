using System.Net;

namespace ContentIntelligencePlatform.Infrastructure.Tests.TestSupport;

/// <summary>
/// Handler falso para testear clientes HTTP sin llamar a la red real.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseBody;
    public HttpRequestMessage? LastRequest { get; private set; }

    public FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
    {
        _statusCode = statusCode;
        _responseBody = responseBody;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody)
        };
        return Task.FromResult(response);
    }
}

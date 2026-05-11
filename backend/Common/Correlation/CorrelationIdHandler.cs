using Microsoft.AspNetCore.Http;

namespace PRC.Common.Correlation;

/// <summary>
/// Propagates X-Correlation-Id from the current request to all outgoing HttpClient calls.
/// Register via AddHttpMessageHandler&lt;CorrelationIdHandler&gt;() on any named/typed HttpClient.
/// </summary>
public class CorrelationIdHandler : DelegatingHandler
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly IHttpContextAccessor _http;

    public CorrelationIdHandler(IHttpContextAccessor http) => _http = http;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var id = _http.HttpContext?.Items["CorrelationId"]?.ToString()
              ?? _http.HttpContext?.TraceIdentifier;

        if (!string.IsNullOrEmpty(id) && !request.Headers.Contains(HeaderName))
            request.Headers.TryAddWithoutValidation(HeaderName, id);

        return base.SendAsync(request, ct);
    }
}

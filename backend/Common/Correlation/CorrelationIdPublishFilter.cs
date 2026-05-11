using MassTransit;
using Microsoft.AspNetCore.Http;

namespace PRC.Common.Correlation;

/// <summary>
/// Adds X-Correlation-Id to every published and sent MassTransit message header
/// so consumers can log it and propagate it further.
/// </summary>
public class CorrelationIdPublishFilter<T> : IFilter<PublishContext<T>> where T : class
{
    private const string HeaderKey = "X-Correlation-Id";
    private readonly IHttpContextAccessor? _http;

    public CorrelationIdPublishFilter(IHttpContextAccessor? http = null) => _http = http;

    public Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        SetHeader(context);
        return next.Send(context);
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("correlationId");

    private void SetHeader(SendContext context)
    {
        if (context.Headers.TryGetHeader(HeaderKey, out _)) return;

        var id = _http?.HttpContext?.Items["CorrelationId"]?.ToString()
              ?? _http?.HttpContext?.TraceIdentifier;

        if (!string.IsNullOrEmpty(id))
            context.Headers.Set(HeaderKey, id);
    }
}

public class CorrelationIdSendFilter<T> : IFilter<SendContext<T>> where T : class
{
    private const string HeaderKey = "X-Correlation-Id";
    private readonly IHttpContextAccessor? _http;

    public CorrelationIdSendFilter(IHttpContextAccessor? http = null) => _http = http;

    public Task Send(SendContext<T> context, IPipe<SendContext<T>> next)
    {
        if (!context.Headers.TryGetHeader(HeaderKey, out _))
        {
            var id = _http?.HttpContext?.Items["CorrelationId"]?.ToString()
                  ?? _http?.HttpContext?.TraceIdentifier;
            if (!string.IsNullOrEmpty(id))
                context.Headers.Set(HeaderKey, id);
        }
        return next.Send(context);
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("correlationId");
}

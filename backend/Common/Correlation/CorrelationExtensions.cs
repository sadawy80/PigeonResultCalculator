using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace PRC.Common.Correlation;

public static class CorrelationExtensions
{
    /// <summary>
    /// Registers <see cref="CorrelationIdHandler"/> as a transient DelegatingHandler
    /// so it can be attached to any named or typed HttpClient via
    /// <c>.AddHttpMessageHandler&lt;CorrelationIdHandler&gt;()</c>.
    /// </summary>
    public static IServiceCollection AddCorrelationIdHandler(this IServiceCollection services)
    {
        services.AddTransient<CorrelationIdHandler>();
        return services;
    }

    /// <summary>
    /// Wires the X-Correlation-Id MassTransit publish/send pipeline filters so
    /// all outgoing messages carry the current request's correlation id header.
    /// Call inside <c>x.UsingRabbitMq((ctx, cfg) =&gt; { ... })</c>.
    /// </summary>
    public static void UseCorrelationIdFilters(this IBusFactoryConfigurator cfg, IBusRegistrationContext ctx)
    {
        cfg.UsePublishFilter(typeof(CorrelationIdPublishFilter<>), ctx);
        cfg.UseSendFilter(typeof(CorrelationIdSendFilter<>), ctx);
    }
}

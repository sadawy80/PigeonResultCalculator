using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace PRC.ApiGateway.Consul;

public sealed class ConsulProxyConfig : IProxyConfig
{
    private readonly CancellationToken _changeToken;

    public ConsulProxyConfig(
        IReadOnlyList<RouteConfig> routes,
        IReadOnlyList<ClusterConfig> clusters,
        CancellationToken changeToken)
    {
        Routes      = routes;
        Clusters    = clusters;
        ChangeToken = new CancellationChangeToken(changeToken);
        _changeToken = changeToken;
    }

    public IReadOnlyList<RouteConfig> Routes    { get; }
    public IReadOnlyList<ClusterConfig> Clusters { get; }
    public IChangeToken ChangeToken              { get; }
}

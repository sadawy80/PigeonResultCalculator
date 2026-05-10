using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;

namespace PRC.ApiGateway.Consul;

public sealed class ConsulProxyConfigProvider : IProxyConfigProvider, IHostedService, IDisposable
{
    private readonly IConsulClient _consul;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConsulProxyConfigProvider> _logger;
    private volatile ConsulProxyConfig _config;
    private CancellationTokenSource _changeCts = new();
    private Timer? _timer;
    private readonly HashSet<string> _unhealthyClusters = [];

    public ConsulProxyConfigProvider(
        IConsulClient consul,
        IConfiguration configuration,
        ILogger<ConsulProxyConfigProvider> logger)
    {
        _consul        = consul;
        _configuration = configuration;
        _logger        = logger;
        _config        = BuildConfig([]);
    }

    public IProxyConfig GetConfig() => _config;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(_ => _ = RefreshAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async Task RefreshAsync()
    {
        try
        {
            var clusters = await BuildClustersFromConsulAsync();
            var oldCts   = _changeCts;
            _changeCts   = new CancellationTokenSource();
            _config      = BuildConfig(clusters);
            oldCts.Cancel();
            oldCts.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Consul refresh failed — keeping previous config");
        }
    }

    private ConsulProxyConfig BuildConfig(IReadOnlyList<ClusterConfig> clusters)
        => new(LoadRoutes(), clusters, _changeCts.Token);

    private IReadOnlyList<RouteConfig> LoadRoutes()
    {
        return _configuration.GetSection("ReverseProxy:Routes")
            .GetChildren()
            .Select(s => new RouteConfig
            {
                RouteId   = s.Key,
                ClusterId = s["ClusterId"] ?? "",
                Match     = new RouteMatch { Path = s["Match:Path"] }
            })
            .ToList();
    }

    private async Task<IReadOnlyList<ClusterConfig>> BuildClustersFromConsulAsync()
    {
        var clusterIds = _configuration
            .GetSection("ReverseProxy:Routes")
            .GetChildren()
            .Select(s => s["ClusterId"])
            .OfType<string>()
            .Distinct();

        var clusters = new List<ClusterConfig>();

        foreach (var clusterId in clusterIds)
        {
            // convention: "identity-cluster" → "identity-service"
            var serviceName = clusterId.Replace("-cluster", "-service");

            try
            {
                var result      = await _consul.Health.Service(serviceName, tag: "", passingOnly: true);
                var destinations = result.Response
                    .Select((entry, i) => KeyValuePair.Create(
                        $"{serviceName}-{i}",
                        new DestinationConfig
                        {
                            Address = $"http://{entry.Service.Address}:{entry.Service.Port}"
                        }))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                clusters.Add(new ClusterConfig
                {
                    ClusterId    = clusterId,
                    Destinations = destinations.Count > 0 ? destinations : null
                });

                if (destinations.Count > 0)
                {
                    if (_unhealthyClusters.Remove(clusterId))
                        _logger.LogInformation("Cluster {Cluster}: recovered — {Count} healthy instance(s)", clusterId, destinations.Count);
                }
                else
                {
                    if (_unhealthyClusters.Add(clusterId))
                        _logger.LogWarning("Cluster {Cluster}: no healthy instances in Consul — returning 503", clusterId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query Consul for {Service}", serviceName);
            }
        }

        return clusters;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _changeCts.Dispose();
    }
}

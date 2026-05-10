using Consul;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PRC.Common.Consul;

public sealed class ConsulRegistrationService : IHostedService
{
    private readonly IConsulClient _consul;
    private readonly string _serviceName;
    private readonly int _port;
    private readonly ILogger<ConsulRegistrationService> _logger;
    private string? _serviceId;

    public ConsulRegistrationService(
        IConsulClient consul,
        string serviceName,
        int port,
        ILogger<ConsulRegistrationService> logger)
    {
        _consul = consul;
        _serviceName = serviceName;
        _port = port;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _serviceId = $"{_serviceName}-{Guid.NewGuid():N}";

        var registration = new AgentServiceRegistration
        {
            ID      = _serviceId,
            Name    = _serviceName,
            Address = _serviceName,  // Docker service name resolves via internal DNS
            Port    = _port,
            Check   = new AgentServiceCheck
            {
                HTTP                            = $"http://{_serviceName}:{_port}/health",
                Interval                        = TimeSpan.FromSeconds(15),
                Timeout                         = TimeSpan.FromSeconds(5),
                DeregisterCriticalServiceAfter  = TimeSpan.FromMinutes(2)
            }
        };

        try
        {
            await _consul.Agent.ServiceRegister(registration, cancellationToken);
            _logger.LogInformation(
                "[Consul] REGISTERED {Service} id={Id} address={Address}:{Port} healthCheck={HealthCheck}",
                _serviceName, _serviceId, _serviceName, _port,
                $"http://{_serviceName}:{_port}/health");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "[Consul] REGISTRATION FAILED for {Service} — service will not receive traffic via gateway. Error: {Error}",
                _serviceName, ex.Message);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_serviceId is null) return;
        try
        {
            await _consul.Agent.ServiceDeregister(_serviceId, cancellationToken);
            _logger.LogInformation("[Consul] DEREGISTERED {Service} ({Id})", _serviceName, _serviceId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[Consul] DEREGISTRATION FAILED for {Service}: {Error}", _serviceName, ex.Message);
        }
    }
}

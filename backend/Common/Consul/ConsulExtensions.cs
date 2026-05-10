using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PRC.Common.Consul;

public static class ConsulExtensions
{
    public static IServiceCollection AddConsulServiceRegistration(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        int port)
    {
        services.AddSingleton<IConsulClient>(_ =>
            new ConsulClient(cfg =>
                cfg.Address = new Uri(configuration["Consul:Address"] ?? "http://consul:8500")));

        services.AddHostedService(sp => new ConsulRegistrationService(
            sp.GetRequiredService<IConsulClient>(),
            serviceName,
            port,
            sp.GetRequiredService<ILogger<ConsulRegistrationService>>()));

        return services;
    }
}

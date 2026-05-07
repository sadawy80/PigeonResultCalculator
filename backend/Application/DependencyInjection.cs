using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PigeonRacing.Application.Features.Auth;

namespace PigeonRacing.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }
}

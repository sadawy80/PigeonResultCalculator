using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PRC.Common.Correlation;
using PRC.Common.Messages;
using PRC.NotificationService.Events;
using PRC.NotificationService.Services;
using PRC.Common.Logging;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((sp, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .Destructure.With<PiiDestructuringPolicy>()
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));

    var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
    var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
    var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

    builder.Services.AddMassTransit(x =>
    {
        x.SetKebabCaseEndpointNameFormatter();

        x.AddConsumer<SendEmailEventConsumer>();
        x.AddConsumer<RaceResultsPublishedConsumer>();
        x.AddConsumer<MemberInvitedConsumer>();
        x.AddConsumer<SubscriptionActivatedConsumer>();
        x.AddConsumer<SubscriptionExpiredConsumer>();
        x.AddConsumer<SubscriptionCancelledConsumer>();
        x.AddConsumer<SubscriptionConfirmedEmailConsumer>();
        x.AddConsumer<SubscriptionExpiredEmailConsumer>();
        x.AddConsumer<SubscriptionCancelledEmailConsumer>();
        x.AddConsumer<ExternalLinkRequestedConsumer>();

        x.AddRequestClient<GetUserEmailsRequest>();

        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host(rabbitHost, "/", h =>
            {
                h.Username(rabbitUser);
                h.Password(rabbitPass);
            });
            cfg.UseCorrelationIdFilters(ctx);
            cfg.ConfigureEndpoints(ctx);
        });
    });

    // No-op HttpContextAccessor so MassTransit CorrelationIdFilters can resolve.
    builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

    builder.Services.AddSingleton<IEmailService, SmtpEmailService>();

    IHost host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "NotificationService terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

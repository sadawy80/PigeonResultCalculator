using System.Text;
using Consul;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PRC.ApiGateway.Consul;
using PRC.Common.Correlation;
using Prometheus;
using Serilog;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(ctx.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));

// ── JWT validation ────────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = new[]
{
    builder.Configuration["App:FrontendUrl"] ?? "https://localhost:4300",
    "https://localhost:4300",
};
builder.Services.AddCors(opts => opts.AddPolicy("FrontendCors",
    p => p.WithOrigins(allowedOrigins)
        .AllowAnyHeader().WithMethods("GET","POST","PUT","DELETE","PATCH","OPTIONS").AllowCredentials()));

// ── Consul client ─────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IConsulClient>(_ =>
    new ConsulClient(cfg =>
        cfg.Address = new Uri(builder.Configuration["Consul:Address"] ?? "http://consul:8500")));

// ── YARP + Consul service discovery ──────────────────────────────────────────
builder.Services.AddSingleton<ConsulProxyConfigProvider>();
builder.Services.AddSingleton<IProxyConfigProvider>(sp =>
    sp.GetRequiredService<ConsulProxyConfigProvider>());
builder.Services.AddHostedService(sp =>
    sp.GetRequiredService<ConsulProxyConfigProvider>());
builder.Services.AddReverseProxy();

var app = builder.Build();

app.UseCorrelationId();
app.UseCors("FrontendCors");
app.UseHttpMetrics();
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();
app.MapMetrics("/metrics");
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "ApiGateway" }));

app.Run();

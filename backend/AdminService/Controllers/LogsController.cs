using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PRC.AdminService.Controllers;

[Route("api/logs")]
[AllowAnonymous]
[ApiController]
public class LogsController : ControllerBase
{
    private readonly ILogger<LogsController> _logger;

    public LogsController(ILogger<LogsController> logger) => _logger = logger;

    [HttpPost]
    public IActionResult Ingest([FromBody] ClientLogBatch batch)
    {
        if (batch?.Events == null || batch.Events.Count == 0) return Ok();

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString()
            ?? HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? "unknown";

        foreach (var e in batch.Events)
        {
            var level = e.Level?.ToLowerInvariant() switch
            {
                "verbose" or "trace" or "debug" => LogLevel.Debug,
                "warning" or "warn"             => LogLevel.Warning,
                "error"                         => LogLevel.Error,
                "fatal" or "critical"           => LogLevel.Critical,
                _                               => LogLevel.Information
            };

            using var scope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["SourceContext"]     = e.SourceContext ?? "Angular",
                ["ClientIp"]         = clientIp,
                ["ClientUserAgent"]  = e.UserAgent ?? HttpContext.Request.Headers.UserAgent.ToString(),
                ["ClientCountry"]    = e.Country,
                ["ClientCity"]       = e.City,
                ["SessionId"]        = e.SessionId,
                ["UserId"]           = e.UserId,
                ["PageUrl"]          = e.PageUrl,
                ["AppVersion"]       = e.AppVersion,
                ["Browser"]          = e.Browser,
                ["Os"]               = e.Os,
                ["ScreenResolution"] = e.ScreenResolution,
                ["Viewport"]         = e.Viewport
            });

            _logger.Log(level, e.Exception != null ? new Exception(e.Exception) : null,
                e.MessageTemplate ?? e.Message ?? "(no message)",
                e.Properties?.Values.ToArray() ?? Array.Empty<object?>());
        }

        return Ok();
    }
}

public class ClientLogBatch
{
    public List<ClientLogEvent> Events { get; set; } = new();
}

public class ClientLogEvent
{
    public string? Timestamp        { get; set; }
    public string? Level            { get; set; }
    public string? Message          { get; set; }
    public string? MessageTemplate  { get; set; }
    public string? Exception        { get; set; }
    public string? SourceContext    { get; set; }
    public string? SessionId        { get; set; }
    public string? UserId           { get; set; }
    public string? PageUrl          { get; set; }
    public string? AppVersion       { get; set; }
    public string? UserAgent        { get; set; }
    public string? Browser          { get; set; }
    public string? Os               { get; set; }
    public string? Country          { get; set; }
    public string? City             { get; set; }
    public string? ScreenResolution { get; set; }
    public string? Viewport         { get; set; }
    public Dictionary<string, object?>? Properties { get; set; }
}

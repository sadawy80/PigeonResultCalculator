using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using PRC.RenderingService.Models;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace PRC.RenderingService.Services;

public interface IResultRenderer
{
    Task<byte[]> RenderAsync(ResultType type, ResultRenderRequest req, CancellationToken ct = default);
}

public class ResultRenderer : IResultRenderer
{
    // Result tables are heavier than certs — keep concurrency lower per spec guidance.
    private static readonly SemaphoreSlim _slots = new(2, 2);
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IPuppeteerBrowserHost _browserHost;
    private readonly IWebHostEnvironment _env;

    public ResultRenderer(IPuppeteerBrowserHost browserHost, IWebHostEnvironment env)
    {
        _browserHost = browserHost;
        _env = env;
    }

    public async Task<byte[]> RenderAsync(ResultType type, ResultRenderRequest req, CancellationToken ct = default)
    {
        var (templateName, prefix) = TemplateMeta(type);
        var templatePath = Path.Combine(_env.WebRootPath, "templates", templateName);
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Result template not found: {templatePath}");

        var html     = await File.ReadAllTextAsync(templatePath, ct);
        var dataJson = req.Data.GetRawText();
        var design   = JsonSerializer.Serialize(req.DesignId, _jsonOpts);
        var lang     = JsonSerializer.Serialize(req.Language, _jsonOpts);
        var inject   = $"<script>window.{prefix}_DESIGN={design};window.{prefix}_LANG={lang};window.{prefix}_DATA={dataJson};</script>";
        html = html.Replace("</head>", inject + "\n</head>");

        var browser = await _browserHost.GetAsync(ct);
        await _slots.WaitAsync(ct);
        try
        {
            await using var page = await browser.NewPageAsync();
            await page.SetViewportAsync(new ViewPortOptions
            {
                Width = 794, Height = 1123, DeviceScaleFactor = 2
            });

            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Networkidle0],
                Timeout = 60_000          // larger payloads, longer wait
            });

            await page.WaitForFunctionAsync(
                "() => document.body.getAttribute('data-render-status') === 'complete' || document.body.getAttribute('data-render-status') === 'error'",
                new WaitForFunctionOptions { Timeout = 30_000 });

            var status = await page.EvaluateExpressionAsync<string?>("document.body.getAttribute('data-render-status')");
            if (status == "error")
            {
                var msg = await page.EvaluateExpressionAsync<string?>("document.body.getAttribute('data-error-message')");
                throw new InvalidOperationException($"Result template render error: {msg ?? "(unknown)"}");
            }

            return await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                Landscape = false,
                PrintBackground = true,
                PreferCSSPageSize = true,
                MarginOptions = new MarginOptions { Top = "0", Right = "0", Bottom = "0", Left = "0" }
            });
        }
        finally { _slots.Release(); }
    }

    private static (string TemplateName, string GlobalsPrefix) TemplateMeta(ResultType type) => type switch
    {
        ResultType.Race     => ("race_results.html",    "RACE"),
        ResultType.Ace      => ("ace_result.html",      "ACE"),
        ResultType.SuperAce => ("superace_result.html", "SUPERACE"),
        ResultType.BestLoft => ("bestloft_result.html", "BESTLOFT"),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}

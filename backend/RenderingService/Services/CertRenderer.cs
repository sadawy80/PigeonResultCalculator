using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using PRC.RenderingService.Models;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace PRC.RenderingService.Services;

public interface ICertRenderer
{
    Task<byte[]> RenderAsync(CertType type, CertRenderRequest req, CancellationToken ct = default);
}

public class CertRenderer : ICertRenderer
{
    private static readonly SemaphoreSlim _slots = new(4, 4);
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IPuppeteerBrowserHost _browserHost;
    private readonly IWebHostEnvironment _env;

    public CertRenderer(IPuppeteerBrowserHost browserHost, IWebHostEnvironment env)
    {
        _browserHost = browserHost;
        _env = env;
    }

    public async Task<byte[]> RenderAsync(CertType type, CertRenderRequest req, CancellationToken ct = default)
    {
        var landscape = IsLandscapeDesign(req.DesignId);
        var templatePath = Path.Combine(_env.WebRootPath, "templates", TemplateName(type, landscape));
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Cert template not found: {templatePath}");

        var html     = await File.ReadAllTextAsync(templatePath, ct);
        var dataJson = req.Data.GetRawText(); // pass payload through verbatim — schema lives in the templates
        var design   = JsonEncodedText.Encode(req.DesignId).ToString();
        var lang     = JsonEncodedText.Encode(req.Language).ToString();
        var inject   = $"<script>window.CERT_DESIGN={JsonString(design)};window.CERT_LANG={JsonString(lang)};window.CERT_DATA={dataJson};</script>";
        html = html.Replace("</head>", inject + "\n</head>");

        var browser = await _browserHost.GetAsync(ct);
        await _slots.WaitAsync(ct);
        try
        {
            await using var page = await browser.NewPageAsync();
            await page.SetViewportAsync(new ViewPortOptions
            {
                Width  = landscape ? 1123 : 794,
                Height = landscape ? 794  : 1123,
                DeviceScaleFactor = 2
            });

            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Networkidle0],
                Timeout = 30_000
            });

            // Templates flip body[data-render-status] to "complete" once fonts + QR are ready.
            await page.WaitForFunctionAsync(
                "() => document.body.getAttribute('data-render-status') === 'complete' || document.body.getAttribute('data-render-status') === 'error'",
                new WaitForFunctionOptions { Timeout = 20_000 });

            var status = await page.EvaluateExpressionAsync<string?>("document.body.getAttribute('data-render-status')");
            if (status == "error")
            {
                var msg = await page.EvaluateExpressionAsync<string?>("document.body.getAttribute('data-error-message')");
                throw new InvalidOperationException($"Cert template render error: {msg ?? "(unknown)"}");
            }

            return await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                Landscape = landscape,
                PrintBackground = true,
                PreferCSSPageSize = true,
                MarginOptions = new MarginOptions { Top = "0", Right = "0", Bottom = "0", Left = "0" }
            });
        }
        finally { _slots.Release(); }
    }

    private static string TemplateName(CertType type, bool landscape)
    {
        var orient = landscape ? "landscape" : "portrait";
        return type switch
        {
            CertType.Race     => $"race_cert_{orient}.html",
            CertType.Ace      => $"ace_cert_{orient}.html",
            CertType.SuperAce => $"superace_cert_{orient}.html",
            CertType.BestLoft => $"bestloft_cert_{orient}.html",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    /// <summary>
    /// Landscape design IDs end with 'L' after the numeric position. Examples:
    /// R10L, AR-R3L, L1L, S10L. Portrait IDs never end with 'L' immediately
    /// after a digit because none of the prefixes do.
    /// </summary>
    private static bool IsLandscapeDesign(string designId)
    {
        if (string.IsNullOrEmpty(designId) || !designId.EndsWith('L')) return false;
        // Strip trailing L and ensure char before is a digit.
        var stem = designId[..^1];
        return stem.Length > 0 && char.IsDigit(stem[^1]);
    }

    private static string JsonString(string s) => JsonSerializer.Serialize(s, _jsonOpts);
}

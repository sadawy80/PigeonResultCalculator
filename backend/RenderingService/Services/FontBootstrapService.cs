using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;

namespace PRC.RenderingService.Services;

/// <summary>
/// Downloads every font referenced by the certificate / result templates into
/// <c>wwwroot/fonts/</c> at startup, then writes a single <c>all.css</c> with
/// <c>@font-face</c> rules pointing at the local woff2 files. After this runs
/// the templates can resolve <c>&lt;link href="/fonts/all.css"&gt;</c> without
/// touching the Google Fonts CDN at render time — important because the
/// headless Chromium runs offline in many production environments.
///
/// Runs once: if <c>fonts/all.css</c> already exists and matches the current
/// manifest version, the service exits immediately. The same logic can be
/// invoked offline via <c>tools/FontDownloader</c> to pre-bundle the fonts
/// into the repository so containers start without an outbound HTTP call.
/// </summary>
public class FontBootstrapService : IHostedService
{
    public const string ManifestVersion = "v1";  // bump when fonts list changes
    public const string CssFileName     = "all.css";
    public const string SentinelName    = "manifest.txt";

    // Modern Chrome UA — required to make Google return woff2 URLs (default UA
    // returns older TTF fallbacks).
    public const string ChromeUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

    private static readonly Regex FontFaceUrlRegex = new(
        @"url\((https://fonts\.gstatic\.com/[^)]+)\)",
        RegexOptions.Compiled);

    /// <summary>
    /// Every font family the bundled templates reference, with the weights they
    /// actually use. Subsets are passed via <c>&amp;subset=</c> when relevant
    /// (Arabic, Latin-ext, simplified Chinese).
    /// </summary>
    public static readonly FontRequest[] Manifest =
    {
        // Latin display & body
        new("Cinzel",             "wght@400;600;800"),
        new("Cormorant Garamond", "wght@400;600;700"),
        new("DM Sans",            "wght@400;500;700"),
        new("Fraunces",           "opsz,wght@9..144,300;9..144,600;9..144,800"),
        new("IBM Plex Mono",      "wght@400;500"),
        new("Inter Tight",        "wght@400;500;600;700;800"),
        new("JetBrains Mono",     "wght@400;500;700"),
        new("Manrope",            "wght@400;500;700;800"),
        new("Marcellus"),
        new("Oswald",             "wght@400;700"),
        new("Playfair Display",   "wght@400;700;900"),
        new("Source Sans 3",      "wght@400;600;700"),
        new("Syne",               "wght@500;700;800"),

        // Arabic
        new("Amiri",              "wght@400;700"),
        new("Aref Ruqaa",         "wght@400;700"),
        new("Cairo",              "wght@400;600;700;900"),
        new("Noto Naskh Arabic",  "wght@400;500;700"),
        new("Reem Kufi",          "wght@400;500;700"),
        new("Scheherazade New",   "wght@400;500;700"),
        new("Tajawal",            "wght@400;500;700;900"),

        // Persian / Farsi
        new("Vazirmatn",          "wght@400;500;600;700;800"),

        // Simplified Chinese (race results only)
        new("Noto Sans SC",       "wght@400;500;700;900"),
        new("Noto Serif SC",      "wght@400;700;900"),
    };

    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<FontBootstrapService> _log;

    public FontBootstrapService(
        IWebHostEnvironment env,
        IHttpClientFactory httpFactory,
        ILogger<FontBootstrapService> log)
    {
        _env         = env;
        _httpFactory = httpFactory;
        _log         = log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var fontsDir = Path.Combine(_env.WebRootPath, "fonts");
            using var http = _httpFactory.CreateClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd(ChromeUserAgent);

            await DownloadAllAsync(fontsDir, http, message => _log.LogInformation("{Msg}", message), cancellationToken);
        }
        catch (Exception ex)
        {
            _log.LogError(ex,
                "Font bootstrap failed. Templates will fall back to inline Google Fonts CDN " +
                "(works if the host has internet access). Re-run startup with network to bundle locally.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Downloads every font family in <see cref="Manifest"/> into <paramref name="fontsDir"/>
    /// and writes <c>all.css</c> + a <c>manifest.txt</c> sentinel. Skips work
    /// if the sentinel already matches <see cref="ManifestVersion"/>.
    /// </summary>
    public static async Task DownloadAllAsync(string fontsDir, HttpClient http, Action<string>? log = null, CancellationToken ct = default)
    {
        log ??= _ => { };
        Directory.CreateDirectory(fontsDir);

        var sentinel = Path.Combine(fontsDir, SentinelName);
        if (File.Exists(sentinel) &&
            (await File.ReadAllTextAsync(sentinel, ct)).Trim() == ManifestVersion &&
            File.Exists(Path.Combine(fontsDir, CssFileName)))
        {
            log($"Fonts already bundled at {fontsDir} (manifest {ManifestVersion}); skipping download.");
            return;
        }

        log($"Bundling {Manifest.Length} Google Font families into {fontsDir}");

        if (http.DefaultRequestHeaders.UserAgent.Count == 0)
            http.DefaultRequestHeaders.UserAgent.ParseAdd(ChromeUserAgent);

        var combinedCss = new StringBuilder();
        combinedCss.AppendLine($"/* Auto-generated by FontBootstrapService — manifest {ManifestVersion} */");
        combinedCss.AppendLine();

        var downloadedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var req in Manifest)
        {
            try
            {
                var css       = await FetchFontFaceCssAsync(http, req, ct);
                var rewritten = await DownloadReferencedFilesAsync(http, css, fontsDir, downloadedFiles, ct);
                combinedCss.AppendLine($"/* {req.Family} */");
                combinedCss.AppendLine(rewritten);
                combinedCss.AppendLine();
                log($"  ✓ {req.Family}");
            }
            catch (Exception ex)
            {
                log($"  ✗ {req.Family}: {ex.Message}");
            }
        }

        var cssPath = Path.Combine(fontsDir, CssFileName);
        await File.WriteAllTextAsync(cssPath, combinedCss.ToString(), ct);
        await File.WriteAllTextAsync(sentinel, ManifestVersion, ct);

        log($"Wrote {cssPath} with {downloadedFiles.Count} woff2 files");
    }

    private static async Task<string> FetchFontFaceCssAsync(HttpClient http, FontRequest req, CancellationToken ct)
    {
        var family = Uri.EscapeDataString(req.Family).Replace("%20", "+");
        var url = string.IsNullOrEmpty(req.Spec)
            ? $"https://fonts.googleapis.com/css2?family={family}&display=swap"
            : $"https://fonts.googleapis.com/css2?family={family}:{req.Spec}&display=swap";

        using var resp = await http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct);
    }

    private static async Task<string> DownloadReferencedFilesAsync(
        HttpClient http, string css, string fontsDir, HashSet<string> seen, CancellationToken ct)
    {
        var matches = FontFaceUrlRegex.Matches(css);
        var rewritten = new StringBuilder(css);

        foreach (Match m in matches.Reverse())
        {
            var remote = m.Groups[1].Value;
            var fileName = SanitiseFileName(new Uri(remote).LocalPath);
            var localPath = Path.Combine(fontsDir, fileName);

            if (seen.Add(fileName) && !File.Exists(localPath))
            {
                using var fileResp = await http.GetAsync(remote, ct);
                fileResp.EnsureSuccessStatusCode();
                await using var fs = File.Create(localPath);
                await fileResp.Content.CopyToAsync(fs, ct);
            }

            rewritten.Remove(m.Index, m.Length);
            rewritten.Insert(m.Index, $"url(\"/fonts/{fileName}\")");
        }
        return rewritten.ToString();
    }

    private static string SanitiseFileName(string localPath) => Path.GetFileName(localPath);

    public sealed record FontRequest(string Family, string Spec = "");
}

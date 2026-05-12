using System.Runtime.InteropServices;
using PuppeteerSharp;

namespace PRC.RenderingService.Services;

/// <summary>
/// Owns a single headless-Chromium instance shared by every renderer in the
/// process. The first caller pays the launch cost; subsequent callers reuse
/// the same <see cref="IBrowser"/>.
///
/// On Linux ARM (Raspberry Pi, AWS Graviton, Apple-Silicon Linux VMs, etc.)
/// PuppeteerSharp's BrowserFetcher has no Chromium build to download, so we
/// prefer a system-installed browser. Resolution order:
///   1. <c>PUPPETEER_EXECUTABLE_PATH</c> env var, if set
///   2. <c>Puppeteer:ExecutablePath</c> from configuration, if set
///   3. Auto-discovered system binary on Linux ARM (/usr/bin/chromium etc.)
///   4. Fallback: <c>BrowserFetcher.DownloadAsync()</c> (Windows / macOS / Linux x64)
/// </summary>
public interface IPuppeteerBrowserHost
{
    Task<IBrowser> GetAsync(CancellationToken ct = default);
}

public class PuppeteerBrowserHost : IPuppeteerBrowserHost, IAsyncDisposable
{
    private static readonly string[] LinuxChromiumCandidates =
    {
        "/usr/bin/chromium",
        "/usr/bin/chromium-browser",
        "/usr/bin/google-chrome",
        "/usr/bin/google-chrome-stable",
        "/snap/bin/chromium",
        "/usr/lib/chromium/chromium",
        "/usr/lib/chromium-browser/chromium-browser"
    };

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly IConfiguration _config;
    private readonly ILogger<PuppeteerBrowserHost> _log;
    private IBrowser? _browser;

    public PuppeteerBrowserHost(IConfiguration config, ILogger<PuppeteerBrowserHost> log)
    {
        _config = config;
        _log    = log;
    }

    public async Task<IBrowser> GetAsync(CancellationToken ct = default)
    {
        if (_browser is { IsClosed: false }) return _browser;

        await _lock.WaitAsync(ct);
        try
        {
            if (_browser is { IsClosed: false }) return _browser;

            var executablePath = await ResolveExecutablePathAsync(ct);

            var launch = new LaunchOptions
            {
                Headless = true,
                Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage"]
            };
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                launch.ExecutablePath = executablePath;
                _log.LogInformation("Launching Puppeteer with system browser at {Path}", executablePath);
            }
            else
            {
                _log.LogInformation("Launching Puppeteer with BrowserFetcher-managed Chromium");
            }

            _browser = await Puppeteer.LaunchAsync(launch);
            return _browser;
        }
        finally { _lock.Release(); }
    }

    private async Task<string?> ResolveExecutablePathAsync(CancellationToken ct)
    {
        // 1. Environment variable — same convention as upstream puppeteer.
        var envPath = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
            return envPath;

        // 2. Configuration.
        var cfgPath = _config["Puppeteer:ExecutablePath"];
        if (!string.IsNullOrWhiteSpace(cfgPath) && File.Exists(cfgPath))
            return cfgPath;

        // 3. Linux ARM: look for system Chromium because BrowserFetcher has no ARM build.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            (RuntimeInformation.ProcessArchitecture is Architecture.Arm64 or Architecture.Arm))
        {
            foreach (var candidate in LinuxChromiumCandidates)
                if (File.Exists(candidate)) return candidate;

            _log.LogWarning(
                "Linux ARM detected but no system Chromium found in standard paths. " +
                "Install one with: apt-get install -y chromium fonts-noto fonts-noto-color-emoji. " +
                "Falling back to BrowserFetcher, which will likely fail on ARM.");
        }

        // 4. Let BrowserFetcher download Chromium (x64 platforms).
        try
        {
            var fetcher = new BrowserFetcher();
            await fetcher.DownloadAsync();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "BrowserFetcher.DownloadAsync failed; rendering will not be available until a Chromium binary is provisioned manually.");
        }
        return null; // null → Puppeteer uses BrowserFetcher's downloaded binary
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is { IsClosed: false })
            await _browser.CloseAsync();
        _browser?.Dispose();
        _lock.Dispose();
    }
}

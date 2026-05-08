using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace PRC.RenderingService.Services;

public interface IPdfGeneratorService
{
    Task<byte[]> GenerateFromHtmlAsync(string html, CancellationToken ct = default);
}

public class PdfGeneratorService : IPdfGeneratorService
{
    private static readonly SemaphoreSlim _lock = new(1, 1);
    private static IBrowser? _browser;

    public async Task<byte[]> GenerateFromHtmlAsync(string html, CancellationToken ct = default)
    {
        var browser = await GetBrowserAsync(ct);
        await using var page = await browser.NewPageAsync();
        await page.SetContentAsync(html, new NavigationOptions
        {
            WaitUntil = [WaitUntilNavigation.Networkidle0],
            Timeout = 30_000
        });
        return await page.PdfDataAsync(new PdfOptions
        {
            Format = PaperFormat.A4,
            PrintBackground = true,
            MarginOptions = new MarginOptions { Top = "12mm", Bottom = "12mm", Left = "12mm", Right = "12mm" }
        });
    }

    private static async Task<IBrowser> GetBrowserAsync(CancellationToken ct)
    {
        if (_browser != null && !_browser.IsClosed) return _browser;
        await _lock.WaitAsync(ct);
        try
        {
            if (_browser == null || _browser.IsClosed)
            {
                var fetcher = new BrowserFetcher();
                await fetcher.DownloadAsync();
                _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage"]
                });
            }
            return _browser;
        }
        finally { _lock.Release(); }
    }
}

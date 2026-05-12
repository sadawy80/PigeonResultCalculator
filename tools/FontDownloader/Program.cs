using PRC.RenderingService.Services;

// One-shot CLI that runs the same FontBootstrapService logic offline and
// writes the woff2 files + all.css into the RenderingService's wwwroot/fonts
// folder. After this finishes, the result is committed alongside the source
// so containers no longer have to reach fonts.googleapis.com on first boot.
var fontsDir = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "..",
    "backend", "RenderingService", "wwwroot", "fonts"));

Console.WriteLine($"Target directory: {fontsDir}");
using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
await FontBootstrapService.DownloadAllAsync(fontsDir, http, Console.WriteLine);
Console.WriteLine("Done.");

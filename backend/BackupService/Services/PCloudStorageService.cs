namespace PRC.BackupService.Services;

public class PCloudStorageService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly string? _token;
    private readonly string _folder;
    private readonly ILogger<PCloudStorageService> _log;

    public PCloudStorageService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<PCloudStorageService> log)
    {
        _httpFactory = httpFactory;
        _token       = config["PCloud:AccessToken"];
        _folder      = config["PCloud:FolderPath"] ?? "prc-backups";
        _log         = log;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_token);

    public async Task UploadAsync(string fileName, Stream data, CancellationToken ct = default)
    {
        if (!IsConfigured) return;

        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(data), "file", fileName);

        var url = $"https://api.pcloud.com/uploadfile" +
                  $"?path={Uri.EscapeDataString("/" + _folder)}" +
                  $"&access_token={_token}" +
                  $"&filename={Uri.EscapeDataString(fileName)}";

        var http = _httpFactory.CreateClient();
        var resp = await http.PostAsync(url, content, ct);

        if (resp.IsSuccessStatusCode)
            _log.LogInformation("Uploaded {File} to pCloud", fileName);
        else
            _log.LogWarning("pCloud upload failed for {File}: HTTP {Status}", fileName, (int)resp.StatusCode);
    }
}

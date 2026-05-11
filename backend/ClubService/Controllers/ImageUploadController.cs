using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRC.ClubService.Data;
using PRC.Common;
using PRC.Common.Services;
using PRC.Common.Validation;

namespace PRC.ClubService.Controllers;

[Route("api/images")]
[Authorize]
public class ImageUploadController : ClubControllerBase
{
    private readonly IFileStorageService _storage;
    private readonly ClubDbContext _db;

    public ImageUploadController(IFileStorageService storage, ClubDbContext db)
    {
        _storage = storage;
        _db = db;
    }

    [HttpPost("club/{clubId:guid}/logo")]
    [Authorize(Roles = "ClubManager,SuperAdmin")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> UploadClubLogo(Guid clubId, IFormFile file, CancellationToken ct)
    {
        var (valid, error, mime) = ImageValidator.Validate(file.OpenReadStream(), file.Length);
        if (!valid)
            return Problem(detail: error!, statusCode: 400);

        var club = await _db.Clubs.FirstOrDefaultAsync(c => c.Id == clubId, ct);
        if (club is null)
            return NotFound();

        var ext = mime switch
        {
            "image/jpeg" => ".jpg",
            "image/png"  => ".png",
            "image/gif"  => ".gif",
            _            => ".webp"
        };
        var path = await _storage.UploadAsync(file.OpenReadStream(), $"logo{ext}", mime!, "clubs", ct);
        var url  = _storage.GetAccessUrl(path);

        club.LogoUrl = url;
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { url }));
    }

    [HttpGet("files/{*path}")]
    [AllowAnonymous]
    public async Task<IActionResult> Download(string path, CancellationToken ct)
    {
        var stream = await _storage.DownloadAsync(path, ct);
        if (stream is null) return NotFound();

        var ext = Path.GetExtension(path).ToLowerInvariant();
        var mime = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            ".gif"            => "image/gif",
            ".webp"           => "image/webp",
            _                 => "application/octet-stream"
        };
        return File(stream, mime);
    }
}

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
    private readonly IFileServiceClient _files;
    private readonly ClubDbContext _db;

    public ImageUploadController(IFileServiceClient files, ClubDbContext db)
    {
        _files = files;
        _db    = db;
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

        await using var stream = file.OpenReadStream();
        var url = await _files.UploadPublicAsync($"logo{ext}", mime!, stream, file.Length, ct);

        club.LogoUrl = url;
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { url }));
    }
}

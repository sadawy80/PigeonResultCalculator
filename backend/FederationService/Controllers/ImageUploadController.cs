using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.Common.Services;
using PRC.Common.Validation;
using PRC.FederationService.Data;

namespace PRC.FederationService.Controllers;

/// <summary>
/// Federation flag/logo upload — parallel to <c>ClubService.ImageUploadController</c>.
/// Validates JPEG/PNG/WebP/GIF up to 10 MB, forwards through FileService into the
/// public bucket, persists the returned URL on the federation entity.
/// </summary>
[Route("api/images")]
[Authorize]
public class ImageUploadController : FederationControllerBase
{
    private readonly IFileServiceClient _files;
    private readonly FederationDbContext _db;

    public ImageUploadController(IFileServiceClient files, FederationDbContext db)
    {
        _files = files;
        _db    = db;
    }

    [HttpPost("federation/{federationId:guid}/flag")]
    [Authorize(Roles = "FederationManager,SuperAdmin")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> UploadFederationFlag(Guid federationId, IFormFile file, CancellationToken ct)
    {
        var (valid, error, mime) = ImageValidator.Validate(file.OpenReadStream(), file.Length);
        if (!valid)
            return Problem(detail: error!, statusCode: 400);

        var federation = await _db.Federations.FirstOrDefaultAsync(f => f.Id == federationId, ct);
        if (federation is null)
            return NotFound();

        var ext = mime switch
        {
            "image/jpeg" => ".jpg",
            "image/png"  => ".png",
            "image/gif"  => ".gif",
            _            => ".webp"
        };

        await using var stream = file.OpenReadStream();
        var url = await _files.UploadPublicAsync($"flag{ext}", mime!, stream, file.Length, ct);

        federation.FlagUrl = url;
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { url }));
    }
}

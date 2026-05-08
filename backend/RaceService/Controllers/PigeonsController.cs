using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.RaceService.Data;
using PRC.RaceService.DTOs;

namespace PRC.RaceService.Controllers;

[Route("api/pigeons")]
public class PigeonsController : RaceControllerBase
{
    private readonly RaceDbContext _db;

    public PigeonsController(RaceDbContext db) => _db = db;

    [HttpGet("exists")]
    [AllowAnonymous]
    public async Task<IActionResult> Exists([FromQuery] string ringNumber, CancellationToken ct)
    {
        var exists = await _db.Pigeons.AnyAsync(p => p.RingNumber == ringNumber, ct);
        return exists ? Ok() : NotFound();
    }

    [HttpGet("id")]
    [AllowAnonymous]
    public async Task<IActionResult> GetId([FromQuery] string ringNumber, CancellationToken ct)
    {
        var id = await _db.Pigeons
            .Where(p => p.RingNumber == ringNumber)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);
        return Ok(ApiResponse<Guid?>.Ok(id));
    }

    [HttpGet("{pigeonId:guid}")]
    [Authorize]
    public async Task<IActionResult> Get(Guid pigeonId, CancellationToken ct)
    {
        var p = await _db.Pigeons.FirstOrDefaultAsync(x => x.Id == pigeonId, ct);
        if (p == null) return NotFound(ApiResponse<object?>.Fail("Pigeon not found."));
        return Ok(ApiResponse<PigeonDto>.Ok(new PigeonDto(p.Id, p.RingNumber, p.Name, p.Sex, p.YearOfBirth, p.Color, p.Strain, p.PhotoUrl)));
    }
}
